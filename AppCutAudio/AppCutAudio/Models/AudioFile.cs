using NAudio.Lame;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AppCutAudio.Models
{
    public class AudioFile : INotifyPropertyChanged
    {
        // Propiedades para la información básica del archivo de audio
        private string nombreArchivo;
        public string NombreArchivo
        {
            get { return nombreArchivo; }
            set
            {
                if (nombreArchivo != value) { nombreArchivo = value; OnPropertyChanged("NombreArchivo"); }
            }
        }

        private string rutaArchivo;
        public string RutaArchivo
        {
            get { return rutaArchivo; }
            set
            {
                if (rutaArchivo != value) { rutaArchivo = value; OnPropertyChanged("RutaArchivo"); }
            }
        }
        private string title { get; set; }
        public string Title
        {
            get { return title; }
            set { if (title != value) { title = value; OnPropertyChanged("Title"); } }
        }
        private string album { get; set; }
        public string Album
        {
            get { return album; }
            set { if (album != value) { album = value; OnPropertyChanged("Album"); } }
        }
        private string artist { get; set; }
        public string Artist
        {
            get { return artist; }
            set { if (artist != value) { artist = value; OnPropertyChanged("Artist"); } }
        }
        private string comment { get; set; }
        public string Comment
        {
            get { return comment; }
            set { if (comment != value) { comment = value; OnPropertyChanged("Comment"); } }
        }
        private string genre { get; set; }
        public string Genre
        {
            get { return genre; }
            set { if (genre != value) { genre = value; OnPropertyChanged("Genre"); } }
        }
        private string year { get; set; }
        public string Year
        {
            get { return year; }
            set { if (year != value) { year = value; OnPropertyChanged("Year"); } }
        }

        // Constructor
        public AudioFile(string nombreArchivo, string rutaArchivo, ID3TagData iD3)
        {
            NombreArchivo = nombreArchivo;
            RutaArchivo = rutaArchivo;
            Album = iD3.Album;
            Artist = iD3.Artist;
            Comment = iD3.Comment;
            Genre = iD3.Genre;
            Year = iD3.Year;
            Title = iD3.Title;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        internal ID3TagData GetTagData()
        {
            return new ID3TagData()
            {
                Album = this.Album,
                Artist = this.Artist,
                Comment = this.Comment,
                Genre = this.Genre,
                Year = this.Year,
                Title = this.Title
            };
        }
    }
}
