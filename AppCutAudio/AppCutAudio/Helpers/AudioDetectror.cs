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
                WaveFileWriter writer = null;
                int trackNumber = 1;

                while ((read = await Task.Run(() => reader.Read(buffer, 0, buffer.Length))) > 0)
                {
                    float maxVolume = 0f;
                    for (int n = 0; n < read; n++)
                    {
                        var abs = Math.Abs(buffer[n]);
                        if (abs > maxVolume) maxVolume = abs;
                    }

                    // Comienza a grabar si el volumen supera el umbral y aún no está grabando
                    if (maxVolume > 0.1f && !isRecording)
                    {
                        isRecording = true;
                        writer = new WaveFileWriter(new IgnoreDisposeStream(outputStream), reader.WaveFormat);
                    }

                    // Si está grabando y el volumen cae por debajo del umbral, detiene la grabación y guarda la pista
                    if (isRecording && maxVolume <= 0.1f)
                    {
                        isRecording = false;
                        writer?.Dispose();
                        writer = null;

                        // Guarda el contenido de outputStream en un archivo
                        await Task.Run(() => fileIO.WriteAllBytesAsync($"{pathSave}/pista_{trackNumber}.wav", outputStream.ToArray()));
                        outputStream.SetLength(0); // Limpia el MemoryStream para la siguiente pista
                        trackNumber++; // Incrementa el número de pista
                    }

                    // Escribe en el archivo si está grabando
                    if (isRecording)
                    {
                        writer?.WriteSamples(buffer, 0, read);
                    }
                }

                writer?.Dispose();

                // Si quedó alguna pista sin guardar al final del archivo, guárdala ahora
                if (outputStream.Length > 0)
                {
                    await Task.Run(() => fileIO.WriteAllBytesAsync($"{pathSave}/pista_{trackNumber}.wav", outputStream.ToArray()));
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
                string mp3File = nombreArchivo.Replace(".wav", ".mp3");
                await ConvertWavToMp3(nombreArchivo, mp3File, iD3);
                File.Delete(nombreArchivo);

                AudioFile item = new AudioFile(Path.GetFileName(mp3File), mp3File, iD3);

                segmentosMp3.Add(item);
            }

            return segmentosMp3;
        }
    }
}
