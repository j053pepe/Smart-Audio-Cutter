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
using System.Drawing.Imaging;
using System.Drawing;

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
            try
            {
                // Cargar el archivo de audio
                using (var audioFile = new AudioFileReader(path))
                {
                    // Crear un gráfico de amplitud (forma de onda) del audio
                    var samples = new float[audioFile.Length];
                    int bytesRead = audioFile.Read(samples, 0, samples.Length);

                    var bmp = new Bitmap(1400, 400); // Ajusta el tamaño de la figura
                    using (var g = Graphics.FromImage(bmp))
                    {
                        g.Clear(System.Drawing.Color.White);
                        for (int i = 0; i < samples.Length; i++)
                        {
                            int xPixel = (int)Math.Round(i * (double)(bmp.Width - 1) / samples.Length);
                            int yPixel = (int)Math.Round((1 - samples[i]) * (bmp.Height - 1) / 2);
                            bmp.SetPixel(xPixel, yPixel, System.Drawing.Color.Blue);

                            // Verificar si hemos llegado al final de las muestras disponibles
                            if (i == samples.Length - 1)
                            {
                                break; // Detener el bucle si no hay más muestras
                            }
                        }
                    }

                    // Guardar el gráfico como una imagen
                    bmp.Save(pathOutputSpectrogram, ImageFormat.Png);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al generar el Espetrograma");
            }
        }


        (double[] audio, int sampleRate) ReadMono(string filePath, double multiplier = 16_000)
        {
            using var afr = new NAudio.Wave.AudioFileReader(filePath);
            int sampleRate = afr.WaveFormat.SampleRate;
            int bytesPerSample = afr.WaveFormat.BitsPerSample / 8;
            int sampleCount = (int)(afr.Length / bytesPerSample);
            int channelCount = afr.WaveFormat.Channels;
            var audio = new List<double>(sampleCount);
            var buffer = new float[sampleRate * channelCount];
            int samplesRead = 0;
            while ((samplesRead = afr.Read(buffer, 0, buffer.Length)) > 0)
                audio.AddRange(buffer.Take(samplesRead).Select(x => x * multiplier));
            return (audio.ToArray(), sampleRate);
        }
    }
}
