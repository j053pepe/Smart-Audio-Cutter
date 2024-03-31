using AppCutAudio.Models;
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
using System.Windows.Shapes;

namespace AppCutAudio.Vistas
{
    /// <summary>
    /// Lógica de interacción para TrackDetails.xaml
    /// </summary>
    public partial class TrackDetails : Window
    {
        public AudioFile AudioUpdate { get; private set; }
        public TrackDetails(AudioFile audioFile)
        {
            InitializeComponent();
            txtAlbum.Text = audioFile.Album;
            txtArtista.Text = audioFile.Artist;
            txtComentario.Text = audioFile.Comment;
            txtGenero.Text = audioFile.Genre;
            txtNombreArchivo.Text = audioFile.NombreArchivo.Substring(0, audioFile.NombreArchivo.Length - 4);
            txtTitulo.Text = audioFile.Title;
            txtYear.Text = audioFile.Year;
            AudioUpdate = audioFile;
        }

        private void btnGuardarInfo_Click(object sender, RoutedEventArgs e)
        {
            AudioUpdate.Album = txtAlbum.Text;
            AudioUpdate.Artist = txtArtista.Text;
            AudioUpdate.Comment = txtComentario.Text;
            AudioUpdate.Genre = txtGenero.Text;
            AudioUpdate.NombreArchivo = txtNombreArchivo.Text + ".mp3";
            AudioUpdate.Title = txtTitulo.Text;
            AudioUpdate.Year = txtYear.Text;
            DialogResult = true;
        }

        private void btnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void txtYear_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(txtYear.Text, out int year))
            {
                if (year < 1900 || year > DateTime.Now.Year)
                {
                    MessageBox.Show("Por favor, ingresa un año entre 1900 y el año actual.");
                    txtYear.Text = AudioUpdate.Year; // Restablece el valor al año original
                }
            }
            else
            {
                MessageBox.Show("Por favor, ingresa un número válido para el año.");
                txtYear.Text = AudioUpdate.Year; // Restablece el valor al año original
            }
        }
    }
}