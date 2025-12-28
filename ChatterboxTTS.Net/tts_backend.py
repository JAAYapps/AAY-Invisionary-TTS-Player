import random
try:
    from collections.abc import Buffer
except ImportError:
    from typing_extensions import Buffer

import numpy as np
import torch
from chatterbox import ChatterboxTTS

DEVICE = "cuda" if torch.cuda.is_available() else "cpu"

def set_seed(seed: int):
    torch.manual_seed(seed)
    torch.cuda.manual_seed(seed)
    torch.cuda.manual_seed_all(seed)
    random.seed(seed)
    np.random.seed(seed)

def load_model():
    """Loads the ChatterboxTTS model."""
    print("Loading ChatterboxTTS model...")
    model = ChatterboxTTS.from_pretrained(DEVICE)
    print("Model loaded.")
    return model

def generate(model, text, audio_prompt_path, exaggeration, temperature, seed_num, cfgw, min_p, top_p, repetition_penalty) -> tuple[int, Buffer]:
    """Generates audio using the provided parameters."""
    if model is None:
        model = ChatterboxTTS.from_pretrained(DEVICE)

    if seed_num != 0:
        set_seed(int(seed_num))

    print(f"Generating audio for text: '{text[:50]}...'")
    wav = model.generate(
        text,
        audio_prompt_path=audio_prompt_path,
        exaggeration=exaggeration,
        temperature=temperature,
        cfg_weight=cfgw,
        min_p=min_p,
        top_p=top_p,
        repetition_penalty=repetition_penalty,
    )
    print("Audio generation complete.")
    return (model.sr, wav.squeeze(0).numpy())

# NEW function to call from C#
def generate_from_buffer(model, text, audio_prompt_buffer, audio_prompt_sr, exaggeration, temperature, seed_num, cfgw, min_p, top_p, repetition_penalty) -> tuple[int, Buffer]:
    """Generates audio using an in-memory audio buffer."""
    if model is None:
        model = ChatterboxTTS.from_pretrained(DEVICE)

    if seed_num != 0:
        set_seed(int(seed_num))

    print(f"Generating audio for text: '{text[:50]}...' from audio buffer.")

    # Call the new method on the model object
    wav_tensor = model.generate_from_buffer(
        text,
        audio_prompt_buffer=audio_prompt_buffer,
        audio_prompt_sr=audio_prompt_sr,
        exaggeration=exaggeration,
        temperature=temperature,
        cfg_weight=cfgw,
        min_p=min_p,
        top_p=top_p,
        repetition_penalty=repetition_penalty,
    )

    print("Audio generation complete.")
    # The output is already a tensor, so just convert to numpy
    return (model.sr, wav_tensor.squeeze(0).numpy())