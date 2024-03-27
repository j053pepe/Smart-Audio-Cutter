import sys
from pydub import AudioSegment, silence
import os
import numpy as np
import soundfile as sf
from scipy.io.wavfile import write

async def detect_sound_in_audio_file(input_file_path, path_save):
    data, samplerate = sf.read(input_file_path)
    is_recording = False
    track_number = 1
    output_data = []

    for i in range(0, len(data), samplerate):
        chunk = data[i:i+samplerate]
        max_volume = np.max(np.abs(chunk))

        if max_volume > 0.1 and not is_recording:
            is_recording = True

        if is_recording and max_volume <= 0.1:
            is_recording = False
            write(os.path.join(path_save, f'pista_{track_number}.wav'), samplerate, np.array(output_data))
            output_data = []
            track_number += 1

        if is_recording:
            output_data.extend(chunk)

    if len(output_data) > 0:
        write(os.path.join(path_save, f'pista_{track_number}.wav'), samplerate, np.array(output_data))

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
        detect_sound_in_audio_file(ruta_archivo,)

        print("Se acabo")
    except Exception as e:
        print(f"Error al procesar el archivo: {e}")

if __name__ == "__main__":
    main()
