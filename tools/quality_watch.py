"""Read-only Windows replay progress. Never changes focus or adjudicates a pass.

Run one existing PowerShell replay runner and observe its player PID. Foreground
records contain only PID and executable basename, never window titles or input.
Use this inside the same exclusive blocking command as the visible replay.
"""
import argparse
import ctypes
from ctypes import wintypes
from datetime import datetime, timezone
import json
import os
from pathlib import Path
import re
import subprocess
import time


def progress(log, expected_pid, foreground):
    checkpoints = re.findall(r'^QUALITY_CHECKPOINT_BEGIN (.+)$', log, re.M)
    owner = foreground.get('pid')
    return dict(checkpointAttempt=checkpoints[-1].strip() if checkpoints else None,
                validation='REJECTED' if 'QUALITY_REPLAY_REJECTED' in log else 'PENDING',
                expectedPlayerPid=expected_pid, foreground=foreground,
                foregroundMatches=(owner == expected_pid) if owner and expected_pid else None,
                scope='Checkpoint entry is an attempt, not proof of arrival or success. Windows owner observations do not replace Unity per-frame focus validation.')


def foreground_reader():
    user = ctypes.WinDLL('user32', use_last_error=True)
    kernel = ctypes.WinDLL('kernel32', use_last_error=True)
    user.GetForegroundWindow.restype = wintypes.HWND
    user.GetWindowThreadProcessId.argtypes = [wintypes.HWND, ctypes.POINTER(wintypes.DWORD)]
    kernel.OpenProcess.argtypes = [wintypes.DWORD, wintypes.BOOL, wintypes.DWORD]
    kernel.OpenProcess.restype = wintypes.HANDLE
    kernel.QueryFullProcessImageNameW.argtypes = [wintypes.HANDLE, wintypes.DWORD, wintypes.LPWSTR, ctypes.POINTER(wintypes.DWORD)]
    kernel.CloseHandle.argtypes = [wintypes.HANDLE]

    def read():
        pid = wintypes.DWORD()
        window = user.GetForegroundWindow()
        if not window:
            return dict(pid=None, image=None, unavailable='no foreground window')
        user.GetWindowThreadProcessId(window, ctypes.byref(pid))
        result = dict(pid=pid.value or None, image=None)
        handle = kernel.OpenProcess(0x1000, False, pid.value)
        if not handle:
            result['imageUnavailableWinError'] = ctypes.get_last_error()
            return result
        try:
            size = wintypes.DWORD(32768)
            buffer = ctypes.create_unicode_buffer(size.value)
            if kernel.QueryFullProcessImageNameW(handle, 0, buffer, ctypes.byref(size)):
                result['image'] = Path(buffer.value).name
            else:
                result['imageUnavailableWinError'] = ctypes.get_last_error()
        finally:
            kernel.CloseHandle(handle)
        return result
    return read


def tail(path, limit=65536):
    if not path.exists():
        return ''
    with path.open('rb') as stream:
        stream.seek(max(0, path.stat().st_size - limit))
        return stream.read().decode('utf-8', errors='replace')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--runner', type=Path, required=True)
    parser.add_argument('--player-output', type=Path, required=True)
    parser.add_argument('--prefix', type=Path, required=True)
    args = parser.parse_args()
    if os.name != 'nt':
        parser.error('Foreground collection requires Windows')
    if not args.runner.is_file():
        parser.error('Runner is missing')
    log_path = Path(str(args.prefix) + '-launcher.txt')
    records_path = Path(str(args.prefix) + '-foreground.jsonl')
    if log_path.exists() or records_path.exists():
        parser.error('Preserve previous evidence; choose a new prefix')
    read_foreground = foreground_reader()
    next_report = 0
    previous_match = None
    offset = 0
    with log_path.open('xb') as output, records_path.open('x', encoding='utf-8') as records:
        child = subprocess.Popen(['powershell', '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', str(args.runner)],
                                 stdout=output, stderr=subprocess.STDOUT, creationflags=subprocess.CREATE_NO_WINDOW)
        while True:
            code = child.poll()
            with log_path.open('rb') as stream:
                stream.seek(offset)
                chunk = stream.read()
                offset = stream.tell()
            if chunk:
                print(chunk.decode('utf-8', errors='replace'), end='', flush=True)
            pid_file = args.player_output / 'process.txt'
            try:
                pid = int(pid_file.read_text(encoding='utf-8-sig').strip())
            except (OSError, ValueError):
                pid = None
            row = progress(tail(args.player_output / 'player.log'), pid, read_foreground())
            row['observedUtc'] = datetime.now(timezone.utc).isoformat()
            row['runnerExited'] = code is not None
            records.write(json.dumps(row, allow_nan=False) + '\n')
            records.flush()
            changed = row['foregroundMatches'] != previous_match
            if time.monotonic() >= next_report or changed:
                print('READ_ONLY_REPLAY_OBSERVATION ' + json.dumps(row), flush=True)
                next_report = time.monotonic() + 45
            previous_match = row['foregroundMatches']
            if code is not None:
                return code
            time.sleep(2)


if __name__ == '__main__':
    raise SystemExit(main())
