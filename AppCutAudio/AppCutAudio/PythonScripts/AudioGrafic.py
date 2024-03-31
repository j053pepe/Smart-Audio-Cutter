import sys
import logging
import matplotlib.pyplot as plt
from pydub import AudioSegment
import base64
from io import BytesIO
import os
#import pdb; pdb.set_trace()

def main():
    if len(sys.argv) != 2:
        logging.error("Uso: python ruta_de_tu_script.py <ruta_del_archivo>")
        sys.exit(1)

    ruta_archivo = sys.argv[1]
    # Procesa la ruta del archivo como sea necesario
    logging.info(f"Archivo recibido: {ruta_archivo}")
    
    # Obtén el nombre del archivo actual
    nombre_archivoRecibido = os.path.basename(ruta_archivo)
    logging.info(f"Nombre del archivo actual: {nombre_archivoRecibido}")
    
    try:
        # Cargar el archivo de audio (reemplaza 'nombre_del_archivo.mp3' con tu propio archivo)
        audio_file = AudioSegment.from_file(ruta_archivo)
        logging.info("Archivo de audio cargado correctamente.")

        # Crear un gráfico de amplitud (forma de onda) del audio
        samples = audio_file.get_array_of_samples()
        logging.info("Obtenidas las muestras del audio.")

        plt.figure(figsize=(14, 4))  # Ajusta el tamaño de la figura
        plt.plot(samples)
        plt.xlabel("Tiempo")
        plt.ylabel("Amplitud")
        plt.title(f"Primeros 3 minutos con audio (no se toma el silencio)")
        plt.grid(True)
        logging.info("Gráfico creado correctamente.")

        # Guardar el gráfico como una imagen
        plt.savefig("espectrograma.png")
        #logging.info("Gráfico guardado como espectrograma.png")
    except MemoryError as mem_err:
        logging.error(f"Error de memoria: {mem_err}")
    except Exception as e:
        logging.error(f"Error al procesar el archivo: {e}")

if __name__ == "__main__":
    main()
