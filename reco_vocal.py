import speech_recognition as sr
from AudioInterface import AudioInterface

if __name__ == '__main__':

    mic = AudioInterface()
    mic = mic.setDeviceByName()
    recognizer = sr.Recognizer()

    with mic as source:
        recognizer.dynamic_energy_threshold = False
        recognizer.adjust_for_ambient_noise(source)
        print(recognizer.energy_threshold)
        input("Press enter to start recording")
        i = 0
        j = 0
        min_speech_time = 1
        while True:        
            try:
                audio = recognizer.listen(source, timeout=1)
                print("speech")
                speech_time = int(len(audio.frame_data))/(audio.sample_width*audio.sample_rate)
                print(speech_time)
                if(speech_time > min_speech_time):
                    # recognize speech using Google Speech Recognition
                    """try:
                        # for testing purposes, we're just using the default API key
                        # to use another API key, use `r.recognize_google(audio, key="GOOGLE_SPEECH_RECOGNITION_API_KEY")`
                        # instead of `r.recognize_google(audio)`
                        print("Google Speech Recognition thinks you said " + r.recognize_google(audio))
                    except sr.UnknownValueError:
                        print("Google Speech Recognition could not understand audio")
                    except sr.RequestError as e:
                        print("Could not request results from Google Speech Recognition service; {0}".format(e))"""
            except sr.WaitTimeoutError:
                print("silence " + str(j))
                j += 1
                pass
        
