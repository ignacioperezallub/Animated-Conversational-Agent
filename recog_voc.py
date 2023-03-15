import pyaudio
from google.cloud import speech_v1p1beta1 as speech

# check if input is from microphone or audio file
input_type = input("Enter 'microphone' to record from microphone or 'file' to record from audio file: ")

#############################################
#               Microphone                  #
#############################################
if input_type == "microphone":

    # Set up audio stream
    p = pyaudio.PyAudio()
    stream = p.open(format=pyaudio.paInt16, channels=1, rate=16000, input=True, frames_per_buffer=1024)

    # Set up Google Speech-to-Text API client
    client = speech.SpeechClient()
    config = speech.types.RecognitionConfig(language_code='en-US', model='default')
    streaming_config = speech.types.StreamingRecognitionConfig(config=config, interim_results=True)

    # Start audio streaming
    streaming_recognize = client.streaming_recognize(streaming_config)
    audio_generator = stream.read(1024)

    while True:
        # Stream audio data to Google Speech-to-Text API
        audio_input = speech.types.StreamingRecognitionAudio(content=audio_generator)
        streaming_recognize.write(audio_input)

        # Process transcription and generate response
        for response in streaming_recognize:
            if response.results:
                transcript = response.results[0].alternatives[0].transcript
                print(transcript)
                # TODO: Process transcript and generate response

        # Read next chunk of audio data
        audio_generator = stream.read(1024)

#############################################
#               Audio File                  #
#############################################

elif input_type == "file":
    # Open audio file
    with wave.open('sample.mp3', 'rb') as audio_file:
        audio_generator = audio_file.readframes(1024)

        while audio_generator:
            # Stream audio data to Google Speech-to-Text API
            audio_input = speech.types.StreamingRecognitionAudio(content=audio_generator)
            streaming_recognize.write(audio_input)

            # Process transcription and generate response
            for response in streaming_recognize:
                if response.results:
                    transcript = response.results[0].alternatives[0].transcript
                    print(transcript)
                    # TODO: Process transcript and generate response

            # Read next chunk of audio data
            audio_generator = audio_file.readframes(1024)
    
else:
    print("Invalid input type. Please enter 'microphone' or 'file'.")


