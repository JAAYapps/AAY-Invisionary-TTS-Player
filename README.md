# AAY Invisionary TTS Player

*(The spiritual successor to the AAY Text To Speech Player)*

A modern, cross-platform Text-to-Speech (TTS) assistive technology tool designed for visually impaired users. Built with .NET 8 and Avalonia UI, this application leverages powerful AI models to provide high-quality, natural-sounding speech with real-time word highlighting.

---

## Key Features

* **Dual TTS Backends:** Choose between two powerful TTS engines via a simple configuration file:
    * **Python (ChatterboxTTS):** A modern, local AI-driven engine for superior voice quality and custom voice cloning.
    * **EchoGarden:** A flexible, server-based TTS backend.
* **AI-Powered spoken word display on window:** Employs `stable-ts` (powered by OpenAI's Whisper model) to perform precise forced alignment, providing accurate word-by-word display as the text is spoken.
* **Fully Cross-Platform:** Built with Avalonia UI to run natively on **Windows, macOS, and Linux**.
* **Accessible by Design:** Created as an assistive technology tool with a focus on usability for visually impaired users.
* **Audible Error Handling:** Features a unique "Fallback TTS" system. If the main AI backend fails, the application uses a pre-recorded dictionary to audibly report the exact error, ensuring the user is never left in silence.
* **Clipboard Monitoring:** Can automatically read any text copied to the clipboard. This feature can be easily toggled on and off directly from the main window.
* **Save to Audio File:** Export the generated speech to a high-quality audio file (`.wav`, `.ogg`, `.mp3`).
* **Full Playback Control:** Includes play, pause, stop, volume, and speech rate (pitch) controls.
* **Custom Voice Support:** The Python backend supports using a custom audio file as a reference to clone a voice for TTS generation. (`.wav`, `.ogg`, `.mp3`)

## Setup and Installation

This application requires a one-time setup for its backend. Please follow the instructions for the backend you wish to use.

### Backend Selection
Before you run the application for the first time, open the `appsettings.json` file and set the `ChosenBackend` property to either `"Python"` or `"EchoGarden"`.

```
json
{
  "UserSettings": {
    "ChosenBackend": "Python"
  }
}
```

### Python Backend Setup (Recommended)
This is the most powerful backend. It runs locally and provides the highest quality voice and features.

## Prerequisites:

* **.NET 8.0 SDK**
* **Python 3.12 or higher**
* **(For GPU acceleration) An NVIDIA graphics card with CUDA drivers installed. (CPU will take longer to generate audio)**

## Instructions:

1. ### Clone the Repository:
~~~
git clone https://github.com/JAAYapps/AAY-Invisionary-TTS-Player.git
cd InvisionaryTTSPlayer
~~~
2. #### Create and Activate a Python Virtual Environment inside the repo: This is a crucial step to avoid conflicts with other Python projects.

#### Make sure you have Python 3.12
~~~
pip install virtualenv
~~~
#### Create the virtual environment
~~~
python3.12 -m venv .venv-new
~~~
3. ### Activate the environment
### On Windows (PowerShell):
~~~
.\.venv\Scripts\Activate.ps1
~~~
### On Linux/macOS:
~~~
source .venv/bin/activate
~~~
### Your terminal prompt should now show (.venv).

4. ### Install Python Packages:
~~~
pip install -r requirements.txt
~~~
### Note: The first time you run the application, the AI models for ChatterboxTTS and stable-ts will be downloaded automatically. This may take several minutes and requires an internet connection.
5. ### Run the Application:
~~~
dotnet run --project AAYInvisionaryTTSPlayer
~~~
### EchoGarden Backend Setup (Alternative)
### This backend requires the external EchoGarden server to be running.

## Prerequisites:

* **.NET 8.0 SDK**
* **Node.js**

## Instructions:

1. ### (One-Time Only) Configure npm: To avoid permission errors, it is highly recommended to configure npm to use your home directory.

### Tell npm where to install global packages
~~~
npm config set prefix '~/.npm-global'
~~~
2. ### Next, add this new location to your system's PATH. Open your shell configuration file (e.g., ~/.profile) and add this line to the end if you are on linux:
~~~
export PATH=~/.npm-global/bin:$PATH
~~~

### You must close and reopen your terminal for this change to take effect.

3. ### Install EchoGarden Globally:
~~~
npm install -g echogarden
~~~
4. ### Run the EchoGarden Server if debugging is required. Before launching the AAY Invisionary TTS Player, you must start the EchoGarden server in a separate terminal:
~~~
echogarden serve
~~~
5. ### Run the Application: With the server running, open a new terminal and run the player:
~~~
dotnet run --project InvisionaryTTSPlayer/
~~~

# Contributing
### This is an open-source project, and contributions are highly welcome. If you have feature ideas, bug reports, or would like to contribute to the code, please feel free to open an issue or a pull request on our GitHub repository.

# License
### This project is licensed under the MIT License. See the LICENSE file for more details.

# Acknowledgments
### This application is built with the help of many incredible open-source projects, including:
* **Avalonia UI: For the cross-platform user interface.**

* **CommunityToolkit.Mvvm: For the modern MVVM architecture.**

* **Chatterbox-TTS: For the high-quality AI voice synthesis.**

* **stable-ts: For the accurate word alignment.**

* **CSnakes: For the seamless .NET-to-Python bridge.**

* **SFML.Net: For audio playback.**

* **And many more. A full list can be found in the THIRD-PARTY-NOTICES.md file.**
