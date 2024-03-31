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
using System.Windows.Threading;
using System.ComponentModel;
using AppCutAudio.Vistas;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

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
        private readonly DispatcherTimer _timer = new DispatcherTimer();
        private int _currentImageIndex = 0;
        private string[] _imagePaths = { "/Images/Home.jpeg", "/Images/Home1.jpeg", "/Images/Home2.jpeg", "/Images/Home3.jpeg" };
        private string ProyectName;
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
            // Configurar el temporizador para cambiar la imagen cada 30 segundos
            _timer.Interval = TimeSpan.FromSeconds(30);
            _timer.Tick += Timer_Tick;
            _timer.Start();
            // Establecer la primera imagen de fondo
            SetBackgroundImage(_imagePaths[_currentImageIndex]);            
        }

        private async void btnAbrir_Click(object sender, RoutedEventArgs e)
        {
            var dialogo = new Vistas.Dialog();
            if (dialogo.ShowDialog() == true)
            {
                if (ProyectName.ToLower() == "demo")
                {
                    MessageBox.Show("El nombre Demo esta reservado por el programa, prueba con otro nombre.");
                    btnAbrir_Click(sender, e);
                }
                else
                {
                    ProyectName = dialogo.NombreDelProyecto;
                    string carpetaProyecto = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ProyectName);
                    if (Directory.Exists(carpetaProyecto))
                        Directory.Delete(carpetaProyecto, true);

                    string url = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "espectrograma.png");
                    File.Delete(url);

                    btnAbrir.IsEnabled = false;
                    btnLimpiar.IsEnabled = true;
                    Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
                    openFileDialog.Filter = "Audio files (*.mp3;*.m4a;*.wav)|*.mp3;*.m4a;*.wav|All files (*.*)|*.*";
                    if (openFileDialog.ShowDialog() == true)
                    {
                        Mp3FileReader mp3FileReader = new(openFileDialog.FileName);
                        using (Mp3FileReader reader = mp3FileReader)
                        {
                            TimeSpan duration = reader.TotalTime;
                            if (duration.TotalMinutes > 1)
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
                                MessageBox.Show("El archivo de audio seleccionado tiene menos de 1 minutos de duración.");
                            }
                        }
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
            string pathSegmentos = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ProyectName);

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
            if (!string.IsNullOrEmpty(fileName))
            {
                string pathSegmentos = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
                if (Directory.Exists(pathSegmentos))
                    Directory.Delete(pathSegmentos, true);
            }
            InputsHideShow(Visibility.Collapsed);
            lstAudios.ItemsSource = null;
            ListaDeAudios = new ObservableCollection<AudioFile>();
            lstAudios.ItemsSource = ListaDeAudios;
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            waveOut?.Stop();
            PlayFromUrl(pathAudio);
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            waveOut?.Stop();
        }

        private void EditItemListButton_Click(object sender, RoutedEventArgs e)
        {
            // Obtén el elemento seleccionado
            Button playButton = (Button)sender;
            var audio = (AudioFile)playButton.Tag;
            
            // Encuentra el índice del audio en la lista
            int index = ListaDeAudios.IndexOf(audio);

            TrackDetails trackDetailsWindow = new TrackDetails(audio);
            if (trackDetailsWindow.ShowDialog() == true)
            {
                ListaDeAudios[index] = trackDetailsWindow.AudioUpdate;
            }
        }

        private void PlayItemListButton_Click(object sender, RoutedEventArgs e)
        {
            // Obtén el elemento seleccionado
            Button playButton = (Button)sender;
            var audio = (AudioFile)playButton.Tag;
            waveOut?.Stop();
            PlayFromUrl(audio.RutaArchivo);
        }
        private void StopItemListButton_Click(object sender, RoutedEventArgs e)
        {
            waveOut?.Stop();
        }
        private void BtnExportar_Click(object sender, RoutedEventArgs e)
        {
            // Mostrar un MessageBox al usuario
            MessageBox.Show("Por favor, elige la carpeta donde guardar tus archivos generados.");

            // Crear una nueva instancia de FolderBrowserDialog
            System.Windows.Forms.FolderBrowserDialog folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();

            // Mostrar el FolderBrowserDialog y obtener el resultado
            System.Windows.Forms.DialogResult result = folderBrowserDialog.ShowDialog();

            // Si el usuario seleccionó una carpeta y presionó el botón OK
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                // Obtener la carpeta seleccionada
                string selectedPath = folderBrowserDialog.SelectedPath;

                foreach (AudioFile item in ListaDeAudios)
                {
                    AudioHelper.SaveMp3WithID3Tag(item.RutaArchivo, $"{selectedPath}/{item.NombreArchivo}", item.GetTagData());
                }
            }
        }

        private async void btnDemo_Click(object sender, RoutedEventArgs e)
        {
            PlayButton.Visibility = Visibility.Visible;
            StopButton.Visibility = Visibility.Visible;
            btnAbrir.IsEnabled = false;
            btnLimpiar.IsEnabled = true;
            btnExportar.IsEnabled = false;

            string pathDemo = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Demo");
            string finalimage = System.IO.Path.Combine(pathDemo, "finalResult.png");
            pathAudio = $"{pathDemo}/demo.mp3";

            BitmapImage finalBitmap = new BitmapImage(new Uri(finalimage));
            audioImage.Source = finalBitmap;

            await CargarListaDemoAsync(pathDemo);
        }

        #region Helper
        private async Task ShowAudio(Microsoft.Win32.OpenFileDialog openFileDialog)
        {
            string pythonScriptPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PythonScripts", "AudioGrafic.py");
            string carpetaProyecto = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ProyectName);
            string filePath = openFileDialog.FileName;

            if (!File.Exists(pythonScriptPath))
            {
                MessageBox.Show("El archivo de script de Python no se encontró.");
                return;
            }

            try
            {
                //AudioHelper.DividirAudio(filePath, carpetaProyecto, new TimeSpan(0, 30, 0));
                AudioHelper.CrearDemo(filePath, carpetaProyecto, new TimeSpan(0, 3, 0));

                string[] partes = Directory.GetFiles(carpetaProyecto);
                List<BitmapImage> images = new List<BitmapImage>();
                foreach (string parte in partes)
                {
                    using (Process process = new Process())
                    {
                        process.StartInfo.FileName = "python";
                        process.StartInfo.Arguments = $"\"{pythonScriptPath}\" \"{parte}\"";
                        process.StartInfo.RedirectStandardOutput = true;
                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.CreateNoWindow = true;

                        process.Start();
                        string output = await process.StandardOutput.ReadToEndAsync();

                        // Verificar si hay errores de salida
                        if (!string.IsNullOrWhiteSpace(output))
                        {
                            MessageBox.Show($"El script de Python produjo la siguiente salida: {output}");
                        }

                        // Haz algo con la salida del script de Python (si es necesario)
                        // Carga la imagen y asígnala al control Image
                        string imagePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "espectrograma.png");
                        if (File.Exists(imagePath))
                        {
                            BitmapImage bitmap = new BitmapImage(new Uri(imagePath));
                            images.Add(bitmap);
                        }
                        else
                        {
                            MessageBox.Show("La imagen generada por el script de Python no se encontró.");
                        }
                    }
                    string finalimage = System.IO.Path.Combine(carpetaProyecto, "finalResult.png");
                    ImagenHelper.UnirImagenes(images, finalimage);
                    BitmapImage finalBitmap = new BitmapImage(new Uri(finalimage));
                    audioImage.Source = finalBitmap;
                }
            }
            catch (Win32Exception ex)
            {
                MessageBox.Show($"Error al iniciar el proceso de Python: {ex.Message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error inesperado: {ex.Message}");
            }

            overlay.Visibility = Visibility.Hidden;
            PlayButton.Visibility = Visibility.Visible;
            StopButton.Visibility = Visibility.Visible;
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
        private void Timer_Tick(object sender, EventArgs e)
        {
            // Cambiar la imagen de fondo
            _currentImageIndex = (_currentImageIndex + 1) % _imagePaths.Length;
            SetBackgroundImage(_imagePaths[_currentImageIndex]);
        }
        private void SetBackgroundImage(string imagePath)
        {
            imagePath = $"{AppDomain.CurrentDomain.BaseDirectory}{imagePath}";
            try
            {
                imgBrush.ImageSource = new BitmapImage(new Uri(imagePath, UriKind.Relative));
            }
            catch (Exception ex)
            {
                // Manejar errores al cargar la imagen
                MessageBox.Show($"Error al cargar la imagen: {ex.Message}");
            }
        }

        private async Task CargarListaDemoAsync(string pathSegmentos)
        {
            tagData = new NAudio.Lame.ID3TagData()
            {
                Album = "CHUBBY MOSH Trilogía",
                Artist = "David Fau Casquel y Javier Ribes",
                Comment = "3º disco de CHUBBY MOSH",
                Genre = "rock, metal, punk, fussion",
                Year = "2018"
            };
            
            AudioDetector audioDetector = new AudioDetector();

            ObservableCollection<AudioFile> lista = await audioDetector.ConvertirLista(pathSegmentos, tagData);
            foreach (var audio in lista)
            {
                audio.Title = audio.NombreArchivo.Remove(0, 3).Replace(".mp3", "");
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ListaDeAudios.Add(audio);
                });
            }
        }
        #endregion
    }
}
