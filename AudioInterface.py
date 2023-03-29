import pyaudio
from speech_recognition import Microphone

class AudioInterface:
    def __init__(self, chunk = 1024, sample_format = pyaudio.paInt16, sample_rate = 44100):
        self.Audio = pyaudio.PyAudio()
        self.chunk = chunk  #Record in chunks of 1024 samples (Default)
        self.sample_format = sample_format  # 16 bits per sample (Default)
        self.fs = sample_rate  # Record at 44100 samples per second (Default)
        self.device_index = None   #Input device index

    #Get input device index by name and return Microphone instance
    def setDeviceByName(self, device_name = "ADAT (7+8) (RME Fireface UCX)"):
        #Get device count
        index = self.Audio.get_device_count()

        #Target device name
        device_name = device_name

        #Iterate through devices until the target is found as an input
        for i in range(index):
            device = self.Audio.get_device_info_by_index(i)
            if(device["name"] == device_name and device["maxInputChannels"] > 0):
                self.device_index = device["index"]
                print(device)
                break

        return self.setAudioInput()

    #Returns a Microphone entity with the defined parameters
    def setAudioInput(self):
        #Verify input device
        if self.device_index == None:
            print("No input device selected")
            return
        
        #Set up microphone input
        mic = Microphone(device_index=self.device_index,
                         sample_rate=self.fs, chunk_size=self.chunk)
        
        return mic
        
    


    