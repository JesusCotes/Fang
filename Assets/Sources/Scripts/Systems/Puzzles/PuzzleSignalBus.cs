using System;
using System.Collections.Generic;
using UnityEngine;

// Bus central de señales de puzle.
// Desacopla emisores (placas, palancas) de receptores (puertas, plataformas): ninguno
// necesita referencia al otro, solo coincidir en el signalId. Eso permite que una placa
// dispare varias puertas y que una puerta escuche varias fuentes sin cablear arrays.
//
// El estado vigente se guarda aparte de los listeners para que un receptor que se
// suscriba tarde (o que se reactive) reciba el valor actual en vez de esperar al
// siguiente flanco.
//
// Sin persistencia entre escenas: AreaExit recarga la escena y todo vuelve a cero.
// La persistencia real llega con WorldState (Fase 4).
public static class PuzzleSignalBus
{
    private static readonly Dictionary<string, Action<bool>> listeners = new Dictionary<string, Action<bool>>();
    private static readonly Dictionary<string, bool> signals = new Dictionary<string, bool>();

    // Con "Enter Play Mode Options" sin domain reload los estáticos sobreviven a la
    // sesión anterior, así que se limpian explícitamente al arrancar el juego.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        listeners.Clear();
        signals.Clear();
    }

    public static void Subscribe(string signalId, Action<bool> callback)
    {
        if (string.IsNullOrEmpty(signalId) || callback == null) return;

        listeners.TryGetValue(signalId, out Action<bool> current);
        listeners[signalId] = current + callback;
    }

    public static void Unsubscribe(string signalId, Action<bool> callback)
    {
        if (string.IsNullOrEmpty(signalId) || callback == null) return;
        if (!listeners.TryGetValue(signalId, out Action<bool> current)) return;

        current -= callback;

        if (current == null) listeners.Remove(signalId);
        else listeners[signalId] = current;
    }

    // Solo emite en el flanco: emitir el mismo valor dos veces no dispara a nadie.
    public static void SetSignal(string signalId, bool value)
    {
        if (string.IsNullOrEmpty(signalId)) return;

        if (signals.TryGetValue(signalId, out bool previous) && previous == value) return;

        signals[signalId] = value;

        if (listeners.TryGetValue(signalId, out Action<bool> current)) current.Invoke(value);
    }

    public static bool GetSignal(string signalId)
    {
        if (string.IsNullOrEmpty(signalId)) return false;

        return signals.TryGetValue(signalId, out bool value) && value;
    }

    // Útil al recargar un puzle o al reiniciar una sala sin recargar la escena.
    public static void ClearSignals()
    {
        signals.Clear();
    }
}
