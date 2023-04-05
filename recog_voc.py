import pyaudio
import wave
import speech_recognition as sr
#from google.cloud import speech_v1p1beta1 as speech

# Set up speech recognition API client
recognizer = sr.Recognizer()
#recognizer.energy

# check if input is from microphone or audio file
input_type = input("Enter 'mic' to record from microphone or 'file' to record from audio file: ")


#############################################
#               Microphone                  #
#############################################
if input_type == "mic":

    # Set up audio stream
    p = pyaudio.PyAudio()
    
    # List available audio input devices
    print("Available input devices:")
    for i in range(p.get_device_count()):
        info = p.get_device_info_by_index(i)
        if info.get('maxInputChannels') > 0:
            print(f"{i}: {info.get('name')}")
    device_index = input("Select input device with the number and press ENTER")
    # Set up audio stream with selected input device
    stream = p.open(format=pyaudio.paInt16, channels=1, rate=16000, input=True, frames_per_buffer=1024, input_device_index=device_index)

    # Loop variables
    silent_count = 0
    max_silent_count = 10

    # Process audio stream and generate response
    while True:
        audio_generator = stream.read(1024)
        try:
            text = recognizer.recognize_google(audio_generator, language='en-US')
            print("Transcription:", text)
            # TODO: Process text and generate response

            # Reset silent counter if speech is detected
            silent_count = 0

        except sr.UnknownValueError:
            # Increment silent counter if no speech is detected
            silent_count += 1
            if silent_count > max_silent_count:
                break
        except sr.RequestError as e:
            print("Could not request results from Google Speech Recognition service; {0}".format(e))

    # Close the audio stream
    stream.stop_stream()
    stream.close()
    p.terminate()



#############################################
#               Audio File                  #
#############################################

elif input_type == "file":
    # Open audio file
    with sr.AudioFile('sample_audio_speech.wav') as source:
        audio_data = recognizer.record(source)

        # Transcribe audio data
        try:
            text = recognizer.recognize_google(audio_data, language='en-US')
            print("Transcription:", text)
            # TODO: Process text and generate response

        except sr.UnknownValueError:
            print("Google Speech Recognition could not understand audio")
        except sr.RequestError as e:
            print("Could not request results from Google Speech Recognition service; {0}".format(e))
    
else:
    print("Invalid input type. Please enter 'microphone' or 'file'.")


