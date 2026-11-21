"""Faulting thread of a minidump: exception, then a scan of its stack for return addresses, named from the engine's PDB.
usage: dump_stack.py <dump> <folder holding UnityPlayer.dll> <folder holding the pdb> [slots]"""
import ctypes
import struct
import sys

path, image_dir, pdb_dir = sys.argv[1], sys.argv[2], sys.argv[3]
slots = int(sys.argv[4]) if len(sys.argv) > 4 else 700
data = open(path, 'rb').read()
sig, _, count, directory = struct.unpack_from('<4sIII', data, 0)
assert sig == b'MDMP'
streams = {}
for i in range(count):
    kind, size, rva = struct.unpack_from('<III', data, directory + 12 * i)
    streams[kind] = (rva, size)

modules = []
rva, _ = streams[4]
(n,) = struct.unpack_from('<I', data, rva)
for i in range(n):
    base, size, _, _, name_rva = struct.unpack_from('<QIIII', data, rva + 4 + 108 * i)
    (length,) = struct.unpack_from('<I', data, name_rva)
    name = data[name_rva + 4:name_rva + 4 + length].decode('utf-16-le')
    modules.append((base, size, name))


def module_of(address):
    for base, size, name in modules:
        if base <= address < base + size:
            return base, size, name
    return None


rva, _ = streams[6]
thread_id, _, code, flags, record, address, parameters = struct.unpack_from('<IIIIQQI', data, rva)
information = struct.unpack_from('<15Q', data, rva + 40)
context_size, context_rva = struct.unpack_from('<II', data, rva + 8 + 152)
registers = {name: struct.unpack_from('<Q', data, context_rva + offset)[0] for name, offset in
             dict(rax=0x78, rcx=0x80, rdx=0x88, rbx=0x90, rsp=0x98, rbp=0xA0, rsi=0xA8, rdi=0xB0, r8=0xB8, r9=0xC0, rip=0xF8).items()}
print(f'exception {code:#x} at {address:#x} thread {thread_id} access={information[0]} target={information[1]:#x}')
print(' '.join(f'{k}={v:#x}' for k, v in registers.items()))

stack = None
rva, _ = streams[3]
(n,) = struct.unpack_from('<I', data, rva)
for i in range(n):
    tid, _, _, _, _, start, size, memory_rva = struct.unpack_from('<IIIIQQII', data, rva + 4 + 48 * i)
    if tid == thread_id:
        stack = (start, data[memory_rva:memory_rva + size])
print('threads', n, 'stack bytes', len(stack[1]) if stack else None)

dbghelp = ctypes.WinDLL('dbghelp.dll')
handle = ctypes.c_void_p(0x4242)
dbghelp.SymSetOptions(0x2 | 0x10)
dbghelp.SymInitializeW.argtypes = [ctypes.c_void_p, ctypes.c_wchar_p, ctypes.c_int]
dbghelp.SymInitializeW(handle, pdb_dir + ';' + image_dir, 0)
dbghelp.SymLoadModuleExW.argtypes = [ctypes.c_void_p, ctypes.c_void_p, ctypes.c_wchar_p, ctypes.c_wchar_p, ctypes.c_uint64, ctypes.c_uint32, ctypes.c_void_p, ctypes.c_uint32]
dbghelp.SymLoadModuleExW.restype = ctypes.c_uint64
loaded = set()


class SymbolInfo(ctypes.Structure):
    _fields_ = [('SizeOfStruct', ctypes.c_uint32), ('TypeIndex', ctypes.c_uint32), ('Reserved', ctypes.c_uint64 * 2), ('Index', ctypes.c_uint32),
                ('Size', ctypes.c_uint32), ('ModBase', ctypes.c_uint64), ('Flags', ctypes.c_uint32), ('Value', ctypes.c_uint64), ('Address', ctypes.c_uint64),
                ('Register', ctypes.c_uint32), ('Scope', ctypes.c_uint32), ('Tag', ctypes.c_uint32), ('NameLen', ctypes.c_uint32), ('MaxNameLen', ctypes.c_uint32),
                ('Name', ctypes.c_wchar * 512)]


dbghelp.SymFromAddrW.argtypes = [ctypes.c_void_p, ctypes.c_uint64, ctypes.POINTER(ctypes.c_uint64), ctypes.POINTER(SymbolInfo)]


def name_of(address):
    module = module_of(address)
    if module is None:
        return None
    base, size, name = module
    short = name.replace('\\', '/').split('/')[-1]
    if short.lower() in ('unityplayer.dll', 'd3d12core.dll', 'd3d12.dll', 'coronach.exe') and base not in loaded:
        loaded.add(base)
        image = image_dir + '\\' + short if short.lower() in ('unityplayer.dll', 'coronach.exe') else name
        dbghelp.SymLoadModuleExW(handle, None, image, None, base, size, None, 0)
    symbol = SymbolInfo()
    symbol.SizeOfStruct = 88
    symbol.MaxNameLen = 500
    displacement = ctypes.c_uint64(0)
    if base in loaded and dbghelp.SymFromAddrW(handle, address, ctypes.byref(displacement), ctypes.byref(symbol)):
        return f'{short}!{symbol.Name}+{displacement.value:#x}'
    return f'{short}+{address - base:#x}'


print('fault:', name_of(registers['rip']))
if stack:
    start, memory = stack
    offset = registers['rsp'] - start
    shown = 0
    for i in range(slots):
        position = offset + 8 * i
        if position < 0 or position + 8 > len(memory):
            break
        (value,) = struct.unpack_from('<Q', memory, position)
        module = module_of(value)
        if module is None:
            continue
        short = module[2].replace('\\', '/').split('/')[-1].lower()
        if short in ('ntdll.dll',) and shown > 60:
            continue
        print(f'  +{8 * i:#06x} {name_of(value)}')
        shown += 1
        if shown > 70:
            break
