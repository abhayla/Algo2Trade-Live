Imports System.Drawing

Namespace Entities
    Public Interface IPayload
        Property TradingSymbol As String
        'Property OpenPrice As Field
        'Property HighPrice As Field
        'Property LowPrice As Field
        'Property ClosePrice As Field
        'Property Volume As Field
        'Property DailyVolume As Long
        'Property SnapshotDateTime As Date
        'Property PreviousPayload As IPayload
        'ReadOnly Property CandleColor As Color
        'ReadOnly Property CandleRange As Decimal
        'ReadOnly Property CandleRangePercentage As Decimal
        'ReadOnly Property CandleWicks As Wicks
        'Property NumberOfTicks As Integer
        'Property PayloadGeneratedBy As PayloadSource
        'Enum PayloadSource
        '    Tick
        '    Historical
        '    CalculatedTick
        '    CalculatedHistorical
        '    None
        'End Enum
        'Class Wicks
        '    Public Property Top As Double
        '    Public Property Bottom As Double
        'End Class
    End Interface
End Namespace