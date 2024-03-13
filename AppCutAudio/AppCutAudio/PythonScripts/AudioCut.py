import sys
from pydub import AudioSegment, silence
import os

def main():
    if len(sys.argv) != 2:
        print("Uso: python ruta_de_tu_script.py <ruta_del_archivo>")
        sys.exit(1)

    ruta_archivo = sys.argv[1]
    print(f"Archivo recibido: {ruta_archivo}")

    nombre_archivoRecibido = os.path.basename(ruta_archivo)
    print(f"Nombre del archivo actual: {nombre_archivoRecibido}")

    # Crear carpeta
    carpeta_salida = f"segmentos_{nombre_archivoRecibido}"
    os.makedirs(carpeta_salida, exist_ok=True)

    try:
        # Carga tu archivo de audio
        myaudio = AudioSegment.from_mp3(ruta_archivo)

        # Detecta los segmentos de silencio
        silence_segments = silence.detect_silence(myaudio, min_silence_len=1000, silence_thresh=-44)

        for i, (start, stop) in enumerate(silence_segments):
            print(f"Segmento de silencio {i + 1}: Inicio {start} ms, Fin {stop} ms")

        # Convierte los tiempos de inicio y finalización a segundos
        silence_segments_in_seconds = [(start / 1000, stop / 1000) for start, stop in silence_segments]

        # Extrae las pistas que están después de cada segmento de silencio
        for i in range(len(silence_segments_in_seconds) - 1):
            start = silence_segments_in_seconds[i][1]  # Fin del silencio actual
            stop = silence_segments_in_seconds[i + 1][0]  # Inicio del siguiente silencio
            segment = myaudio[int(start * 1000):int(stop * 1000)]
            ruta_segmento = os.path.join(carpeta_salida, f"pista_{i}.mp3")
            segment.export(ruta_segmento, format="mp3")
            print(f"Pista entre silencio {i}: desde {start:.2f} s hasta {stop:.2f} s")

        print("Se acabo")
    except Exception as e:
        print(f"Error al procesar el archivo: {e}")

if __name__ == "__main__":
    main()
