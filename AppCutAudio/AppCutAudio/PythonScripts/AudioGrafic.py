import sys
import matplotlib.pyplot as plt
from pydub import AudioSegment
import base64
from io import BytesIO
import os

def main():
    if len(sys.argv) != 2:
        print("Uso: python ruta_de_tu_script.py <ruta_del_archivo>")
        sys.exit(1)

    ruta_archivo = sys.argv[1]
    # Procesa la ruta del archivo como sea necesario
    print(f"Archivo recibido: {ruta_archivo}")
    
    # Obtén el nombre del archivo actual
    nombre_archivoRecibido = os.path.basename(ruta_archivo)
    print(f"Nombre del archivo actual: {nombre_archivoRecibido}")
    
    try:
        # Cargar el archivo de audio (reemplaza 'nombre_del_archivo.mp3' con tu propio archivo)
        audio_file = AudioSegment.from_file(ruta_archivo)

        # Crear un gráfico de amplitud (forma de onda) del audio
        samples = audio_file.get_array_of_samples()
        plt.figure(figsize=(20, 6))  # Ajusta el tamaño de la figura
        plt.plot(samples)
        plt.xlabel("Tiempo")
        plt.ylabel("Amplitud")
        plt.title(f"Forma de onda del audio {nombre_archivoRecibido}")
        plt.grid(True)

        # Guardar el gráfico como una imagen
        plt.savefig("espectrograma.png")
        print("Gráfico guardado como espectrograma.png")
    except Exception as e:
        print(f"Error al procesar el archivo: {e}")

if __name__ == "__main__":
    main()
