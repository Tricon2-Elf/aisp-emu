namespace AISpace.Network.Data;

/// <summary>
/// recv_event_select_init select_type — client UI variant for in-event selection.
/// </summary>
public enum EventSelectType : uint
{
    /// <summary>IF/CHL dialogue selection window (same style as client CSV if-selection-start).</summary>
    Dialogue = 1,

    /// <summary>Small centered popup (context-menu style).</summary>
    Popup = 2,
}
