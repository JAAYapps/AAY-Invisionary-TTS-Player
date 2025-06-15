import stable_whisper
import json
import argparse
import numpy as np
import librosa

model = None

def _load_model_if_needed():
    """Helper function to load the model only once."""
    global model
    if model is None:
        print("Loading alignment model...")
        model = stable_whisper.load_model('base')
        print("Alignment model loaded.")

def get_word_timestamps_from_buffer(audio_buffer: bytes, sample_rate: int, text: str) -> str:
    """
    Performs forced alignment on an in-memory audio buffer.
    """
    _load_model_if_needed()

    try:
        # 1. Convert the raw bytes from C# (int16) into a NumPy array.
        audio_int16 = np.frombuffer(audio_buffer, dtype=np.int16)

        # 2. Convert to float32 and normalize.
        audio_float32 = audio_int16.astype(np.float32) / 32767.0

        # 3. --- NEW STEP: Resample the audio to 16kHz, which Whisper expects. ---
        # The original sample rate is passed in from our C# app (e.g., 24000).
        if sample_rate != 16000:
            print(f"Resampling audio from {sample_rate}Hz to 16000Hz...")
            audio_16k = librosa.resample(y=audio_float32, orig_sr=sample_rate, target_sr=16000)
        else:
            audio_16k = audio_float32

        # 4. Perform alignment using the resampled 16kHz audio buffer.
        #    NOTE: The `sample_rate` argument has been removed from this call.
        result = model.align(audio_16k, text, language='en')

        word_segments = []
        for segment in result.segments:
            for word in segment.words:
                word_segments.append({
                    "Word": word.word.strip(),
                    "Start": word.start,
                    "End": word.end
                })

        return json.dumps(word_segments)
    except Exception as e:
        print(f"Error during alignment: {e}")
        return "[]"

def get_word_timestamps(audio_path: str, text: str) -> str:
    """
    Performs forced alignment on an audio file and text.

    Returns:
        A JSON string representing a list of word timestamp objects.
    """
    global model
    if model is None:
        print("Loading alignment model...")
        model = stable_whisper.load_model('base')
        print("Alignment model loaded.")

    try:
        result = model.align(audio_path, text, language='en')

        word_segments = []
        for segment in result.segments:
            for word in segment.words:
                word_segments.append({
                    "Word": word.word.strip(), # Use .strip() to clean up whitespace
                    "Start": word.start,
                    "End": word.end
                })

        return json.dumps(word_segments, indent=2) # Use indent for pretty-printing
    except Exception as e:
        print(f"Error during alignment: {e}")
        return "[]"

# This block makes the script runnable from the command line for testing
if __name__ == '__main__':
    # Set up command-line argument parsing
    parser = argparse.ArgumentParser(description="Generate word timestamps for an audio file and text.")
    parser.add_argument("audio_file", help="Path to the audio file (e.g., ../TestConsoleApp/Different issue.wav)")
    parser.add_argument("text", help="The text that is spoken in the audio file.")

    args = parser.parse_args()

    # Call the main function with the provided arguments
    print(f"Aligning '{args.audio_file}'...")
    json_output = get_word_timestamps(args.audio_file, args.text)

    print("\n--- Alignment Result ---")
    print(json_output)