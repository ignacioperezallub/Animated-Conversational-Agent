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
        print(recognizer.pause_threshold)
        input("Enter to start recording")
        i = 0
        j = 0
        min_speech_time = 0.5
        while True:        
            try:
                audio = recognizer.listen(source, timeout=1)
                print("speech")
                speech_time = int(len(audio.frame_data))/(audio.sample_width*audio.sample_rate)
                print(speech_time)
                if(speech_time > min_speech_time):
                    print("Min speech length achieved")
                    with open("microphone-results_"+str(i)+".wav", "wb") as f:
                        f.write(audio.get_wav_data())
                        i += 1
            except sr.WaitTimeoutError:
                print("silence " + str(j))
                j += 1
                pass
        
