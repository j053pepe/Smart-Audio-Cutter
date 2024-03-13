using NAudio.Lame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AppCutAudio.Models
{
    public class AudioFile
    {
        // Propiedades para la información básica del archivo de audio
        public string NombreArchivo { get; set; }
        public string RutaArchivo { get; set; }
        public ID3TagData TagData { get; set; }

        // Constructor
        public AudioFile(string nombreArchivo, string rutaArchivo, ID3TagData iD3)
        {
            NombreArchivo = nombreArchivo;
            RutaArchivo = rutaArchivo;
            TagData = iD3; 
        }
    }
}
