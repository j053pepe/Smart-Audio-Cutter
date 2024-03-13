using Microsoft.Win32;
using NAudio.Wave;
using OxyPlot.Series;
using OxyPlot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.IO;
using AppCutAudio.Helpers;
using NAudio.Lame;
using System.Collections.ObjectModel;
using AppCutAudio.Models;

namespace AppCutAudio
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string pathAudio;
        private string fileName;
        private IWavePlayer waveOut;
        private AudioFileReader audioFile;
        private NAudio.Lame.ID3TagData tagData;
        public ObservableCollection<AudioFile> ListaDeAudios { get; set; }
        public MainWindow()
        {
            InitializeComponent();
            btnProcesar.IsEnabled = false;
            btnLimpiar.IsEnabled = false;
            PlayButton.Visibility = Visibility.Collapsed;
            StopButton.Visibility = Visibility.Collapsed;
            InputsHideShow(Visibility.Collapsed);
            ListaDeAudios = new ObservableCollection<AudioFile>();
            this.DataContext = this;
        }

        private async void btnAbrir_Click(object sender, RoutedEventArgs e)
        {
            string url = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "espectrograma.png");
            File.Delete(url);
            btnAbrir.IsEnabled = false;
            btnLimpiar.IsEnabled = true;
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "Audio files (*.mp3;*.m4a;*.wav)|*.mp3;*.m4a;*.wav|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                using (Mp3FileReader reader = new Mp3FileReader(openFileDialog.FileName))
                {
                    TimeSpan duration = reader.TotalTime;
                    if (duration.TotalMinutes > 3)
                    {
                        btnProcesar.IsEnabled = true;
                        overlay.Visibility = Visibility.Visible;
                        pathAudio = openFileDialog.FileName;
                        fileName = openFileDialog.SafeFileName;
                        await ShowAudio(openFileDialog);
                        InputsHideShow(Visibility.Visible);
                    }
                    else
                    {
                        MessageBox.Show("El archivo de audio seleccionado tiene menos de 3 minutos de duración.");
                    }
                }
            }
        }
        private async void btnProcesar_Click(object sender, RoutedEventArgs e)
        {
            overlay.Visibility = Visibility.Visible;
            tagData = new NAudio.Lame.ID3TagData()
            {
                Album = txtAlbum.Text,
                Artist = txtArtista.Text,
                Comment = txtComment.Text,
                Genre = txtGenero.Text,
                Year = txtYear.Text
            };
            string pathSegmentos = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
            if(Directory.Exists(pathSegmentos))
                Directory.Delete(pathSegmentos, true);
            Directory.CreateDirectory(pathSegmentos);

            // await ProcesarAudio(); //python
            AudioDetector audioDetector = new AudioDetector();
            await audioDetector.DetectSoundInAudioFile(pathAudio, pathSegmentos);

            var lista = await audioDetector.ConvertirLista(pathSegmentos, tagData);
            foreach (var audio in lista)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ListaDeAudios.Add(audio);
                });
            }

            overlay.Visibility = Visibility.Hidden;

        }
        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            audioImage.Source = null;
            btnAbrir.IsEnabled = true;
            btnLimpiar.IsEnabled = false;
            PlayButton.Visibility = Visibility.Collapsed;
            StopButton.Visibility = Visibility.Collapsed;
            btnProcesar.IsEnabled = false;
            string pathSegmentos = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
            Directory.Delete(pathSegmentos, true);
            InputsHideShow(Visibility.Collapsed);
            ListaDeAudios = new ObservableCollection<AudioFile>();
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            PlayFromUrl(pathAudio);
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            waveOut?.Stop();
        }

        private void EditItemListButton_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void PlayItemListButton_Click(object sender, RoutedEventArgs e)
        {
            // Obtén el elemento seleccionado
            Button playButton = (Button)sender;
            var audio = (AudioFile)playButton.Tag;
            PlayFromUrl(audio.RutaArchivo);
        }
        private void StopItemListButton_Click(object sender, RoutedEventArgs e)
        {
            waveOut?.Stop();
        }

        #region Helper
        private async Task ShowAudio(Microsoft.Win32.OpenFileDialog openFileDialog)
        {
            // Obtén la ruta completa al script de Python
            string pythonScriptPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PythonScripts", "AudioGrafic.py");

            // Obtén la ruta del archivo seleccionado
            string filePath = openFileDialog.FileName;

            // Invoca el script de Python con la ruta del archivo como argumento
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "python", // Asegúrate de que "python" esté en tu PATH
                Arguments = $"\"{pythonScriptPath}\" \"{filePath}\"", // Pasa la ruta del archivo como argumento
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = new Process { StartInfo = psi })
            {
                try
                {
                    process.Start();
                    string output = await process.StandardOutput.ReadToEndAsync();
                    // Haz algo con la salida del script de Python (si es necesario)
                    // Carga la imagen y asígnala al control Image
                    string url = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "espectrograma.png");
                    BitmapImage bitmap = new BitmapImage(new Uri(url));
                    audioImage.Source = bitmap;
                    //btnProcesar.IsEnabled = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }

            overlay.Visibility = Visibility.Hidden;
            PlayButton.Visibility = Visibility.Visible;
            StopButton.Visibility = Visibility.Visible;
        }

        private async Task ProcesarAudio()
        {
            // Obtén la ruta completa al script de Python
            string pythonScriptPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PythonScripts", "AudioCut.py");

            // Invoca el script de Python con la ruta del archivo como argumento
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "python", // Asegúrate de que "python" esté en tu PATH
                Arguments = $"\"{pythonScriptPath}\" \"{pathAudio}\"", // Pasa la ruta del archivo como argumento
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = new Process { StartInfo = psi })
            {
                try
                {
                    process.Start();
                    string output = await process.StandardOutput.ReadToEndAsync();

                    int posicionError = output.IndexOf("error", StringComparison.OrdinalIgnoreCase);

                    if (posicionError >= 0)
                    {
                        string errorPart = output.Substring(posicionError);
                        errorPart = errorPart.Trim();
                        throw new Exception($"Error: {errorPart}");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void InputsHideShow(Visibility visibility)
        {
            txtAlbum.Visibility = visibility;
            txtArtista.Visibility = visibility;
            txtComment.Visibility = visibility;
            txtGenero.Visibility = visibility;
            txtYear.Visibility = visibility;
            lblAlbum.Visibility = visibility;
            lblArtista.Visibility = visibility;
            lblComentario.Visibility = visibility;
            lblYear.Visibility = visibility;
            lblComentario.Visibility = visibility;
            lblGenero.Visibility = visibility;
        }

        private void PlayFromUrl(string url)
        {
            waveOut = new WaveOut();
            audioFile = new AudioFileReader(url);

            waveOut.Init(audioFile);
            waveOut.Play();
        }

        #endregion
    }
}
