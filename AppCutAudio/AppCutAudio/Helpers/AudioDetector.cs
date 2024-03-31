using NAudio.Wave;
using NAudio.Lame;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using AppCutAudio.Models;
using ID3TagData = NAudio.Lame.ID3TagData;
using System.Collections.ObjectModel;
using NAudio.Wave.SampleProviders;
using Spectrogram;

namespace AppCutAudio.Helpers
{
    public class AudioDetector
    {
        public async Task DetectSoundInAudioFile(string inputFilePath, string pathSave)
        {
            using (var reader = new AudioFileReader(inputFilePath))
            {
                AsyncFileWriter fileIO = new AsyncFileWriter();
                var sampleRate = reader.WaveFormat.SampleRate;
                var channelCount = reader.WaveFormat.Channels;
                var bufferLength = sampleRate * channelCount;
                var buffer = new float[bufferLength];
                int read;
                bool isRecording = false;
                MemoryStream outputStream = new MemoryStream();
                LameMP3FileWriter writer = null;
                int trackNumber = 1;

                while ((read = await Task.Run(() => reader.Read(buffer, 0, buffer.Length))) > 0)
                {
                    float maxVolume = 0f;
                    for (int n = 0; n < read; n++)
                    {
                        var abs = Math.Abs(buffer[n]);
                        if (abs > maxVolume) maxVolume = abs;
                    }

                    if (maxVolume > 0.1f && !isRecording)
                    {
                        isRecording = true;
                        writer = new LameMP3FileWriter(new IgnoreDisposeStream(outputStream), reader.WaveFormat, LAMEPreset.STANDARD);
                    }

                    if (isRecording && maxVolume <= 0.1f)
                    {
                        isRecording = false;
                        writer?.Dispose();
                        writer = null;

                        await Task.Run(() => fileIO.WriteAllBytesAsync($"{pathSave}/pista_{trackNumber}.mp3", outputStream.ToArray()));
                        outputStream.SetLength(0);
                        trackNumber++;
                    }

                    if (isRecording)
                    {
                        byte[] byteArray = new byte[read * sizeof(float)];
                        Buffer.BlockCopy(buffer, 0, byteArray, 0, byteArray.Length);
                        writer?.Write(byteArray, 0, byteArray.Length);
                    }
                }

                writer?.Dispose();

                if (outputStream.Length > 0)
                {
                    await Task.Run(() => fileIO.WriteAllBytesAsync($"{pathSave}/pista_{trackNumber}.mp3", outputStream.ToArray()));
                }
            }
        }

        private async Task ConvertWavToMp3(string wavFileName, string mp3FileName, ID3TagData iD3)
        {
            using (var reader = new WaveFileReader(wavFileName))
            using (var writer = new LameMP3FileWriter(mp3FileName, reader.WaveFormat, LAMEPreset.VBR_90, iD3))
            {
                await reader.CopyToAsync(writer);
            }
        }

        public async Task<ObservableCollection<AudioFile>> ConvertirLista(string pathFile, ID3TagData iD3)
        {
            List<string> segmentos = Directory.GetFiles(pathFile).ToList();
            ObservableCollection<AudioFile> segmentosMp3 = new ObservableCollection<AudioFile>();

            foreach (string nombreArchivo in segmentos)
            {
                string extencion = nombreArchivo.Split('.').ToList().LastOrDefault();
                if (extencion == "wav")
                {
                    string mp3File = nombreArchivo.Replace(".wav", ".mp3");
                    await ConvertWavToMp3(nombreArchivo, mp3File, iD3);
                    File.Delete(nombreArchivo);

                    AudioFile item = new AudioFile(Path.GetFileName(mp3File), mp3File, iD3);

                    segmentosMp3.Add(item);
                }
                else if (extencion == "mp3")
                {
                    AudioFile item = new AudioFile(Path.GetFileName(nombreArchivo), nombreArchivo, iD3);
                    if (item.NombreArchivo != "demo.mp3")
                        segmentosMp3.Add(item);
                }
            }

            return segmentosMp3;
        }

        public void GenerarEspectrograma(string path, string pathOutputSpectrogram)
        {
            (double[] audio, int sampleRate) = ReadWavMonoNormalized(path);
            var sg = new SpectrogramGenerator(sampleRate, fftSize: 1024, stepSize: 5000, maxFreq: 20000);
            sg.Add(audio);
            sg.GetBitmap().Save(pathOutputSpectrogram);
        }

        public (double[] audio, int sampleRate) ReadWavMonoNormalized(string filePath)
        {
            var reader = new NAudio.Wave.AudioFileReader(filePath);
            var audio = new float[reader.Length];
            reader.Read(audio, 0, audio.Length);
            return (Array.ConvertAll(audio, x => (double)x), reader.WaveFormat.SampleRate);
        }
    }
}
