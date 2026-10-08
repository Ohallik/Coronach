"""Verify bounded listener storage without accepting subjective sound quality."""
from pathlib import Path
import argparse
import json
import math
import struct

MARKER = 'Audio storage: bounded-writer-v1'
MAX_BUFFER_BYTES = 4_325_376


def check(folder: Path) -> dict | None:
    report_path = folder / 'capture-audio.json'
    note = folder / 'capture.txt'
    if not report_path.exists() and (not note.exists() or MARKER not in note.read_text(encoding='utf-8-sig')):
        return None  # Legacy recordings retain their original scope.
    result = {'valid': False, 'failures': [], 'audition': 'UNVERIFIED'}
    try:
        data = json.loads(report_path.read_text(encoding='utf-8-sig'))
        if not isinstance(data, dict):
            raise ValueError('writer report must be an object')
        result['writer'] = data
        fields = ('sampleRate', 'channels', 'bufferCount', 'maximumBlockSamples', 'peakQueuedBlocks',
                  'sampleBufferBytes', 'acceptedSamples', 'writtenSamples', 'writtenBytes')
        if any(type(data.get(key)) is not int or data[key] <= 0 for key in fields):
            raise ValueError('missing or invalid positive writer counts')
        if data.get('completed') is not True or data.get('failure') not in (None, ''):
            raise ValueError('writer incomplete or reported a failure')
        channels, rate, samples = data['channels'], data['sampleRate'], data['writtenSamples']
        if channels > 32 or rate > 768000 or samples % channels:
            raise ValueError('invalid sample format or alignment')
        if samples != data['acceptedSamples'] or data['writtenBytes'] != samples * 4:
            raise ValueError('accepted and written samples differ')
        budget = (data['bufferCount'] + 1) * data['maximumBlockSamples'] * 4
        if data['sampleBufferBytes'] != budget or budget > MAX_BUFFER_BYTES or data['peakQueuedBlocks'] > data['bufferCount']:
            raise ValueError('invalid or exceeded buffer budget')
        path = folder / 'mix.wav'
        if path.stat().st_size != 44 + samples * 4:
            raise ValueError('WAV length disagrees with writer; truncated or trailing audio')
        with path.open('rb') as stream:
            header = stream.read(44)
            expected = struct.pack('<4sI8sIHHIIHH4sI', b'RIFF', 36 + samples * 4, b'WAVEfmt ',
                                   16, 3, channels, rate, rate * channels * 4, channels * 4, 32, b'data', samples * 4)
            if header != expected:
                raise ValueError('WAV header disagrees with listener format or counts')
            for block in iter(lambda: stream.read(262144), b''):
                if any(not math.isfinite(value) for (value,) in struct.iter_unpack('<f', block)):
                    raise ValueError('nonfinite recorded listener sample')
        result['seconds'] = samples / channels / rate
        replay = folder / 'run.json'
        if replay.exists():
            run = json.loads(replay.read_text(encoding='utf-8-sig'))
            seconds = run.get('seconds')
            if type(seconds) not in (int, float) or not math.isfinite(seconds) or seconds <= 0:
                raise ValueError('invalid replay duration for listener coverage')
            result['replaySeconds'] = seconds
            result['durationDifferenceSeconds'] = result['seconds'] - seconds
            # Listener block alignment and one final video frame add a short tail.
            if abs(result['durationDifferenceSeconds']) > .5:
                raise ValueError('listener duration differs from replay by more than 0.5 seconds')
    except (OSError, ValueError, TypeError, KeyError, struct.error) as error:
        result['failures'].append('bounded audio: ' + str(error))
    result['valid'] = not result['failures']
    return result


def include(result: dict, folder: Path) -> None:
    audio = check(folder)
    if audio is not None:
        result['boundedAudio'] = audio
        result['failures'].extend(audio['failures'])
        result['valid'] = not result['failures']


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('folder', type=Path)
    args = parser.parse_args()
    result = check(args.folder)
    if result is None:
        result = {'valid': False, 'failures': ['bounded audio report unavailable; legacy capture']}
    print(json.dumps(result, indent=2))
    raise SystemExit(0 if result['valid'] else 1)
