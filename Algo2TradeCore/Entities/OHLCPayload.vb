Imports System.Drawing

Namespace Entities
    Public Class OHLCPayload
        Implements IPayload

        Public Sub New(ByVal payloadGeneratedBy As PayloadSource)
            Me._PayloadGeneratedBy = payloadGeneratedBy
            Me.OpenPrice = New Field(TypeOfField.Open)
            Me.HighPrice = New Field(TypeOfField.High)
            Me.LowPrice = New Field(TypeOfField.Low)
            Me.ClosePrice = New Field(TypeOfField.Close)
            Me.Volume = New Field(TypeOfField.Volume)
        End Sub
        Public Property TradingSymbol As String Implements IPayload.TradingSymbol
        Public Property OpenPrice As Field 'Implements IPayload.OpenPrice
        Public Property HighPrice As Field 'Implements IPayload.HighPrice
        Public Property LowPrice As Field 'Implements IPayload.LowPrice
        Public Property ClosePrice As Field 'Implements IPayload.ClosePrice
        Public Property Volume As Field 'Implements IPayload.Volume
        Public Property DailyVolume As Long 'Implements IPayload.DailyVolume
        Public Property SnapshotDateTime As Date 'Implements IPayload.SnapshotDateTime
        Public Property PreviousPayload As OHLCPayload 'Implements IPayload.PreviousPayload
        Public Property NumberOfTicks As Integer 'Implements IPayload.NumberOfTicks
        Public Property IsDirtyCandle As Boolean

        Private _PayloadGeneratedBy As PayloadSource
        Public Property PayloadGeneratedBy As PayloadSource 'Implements IPayload.PayloadGeneratedBy
            Get
                Return _PayloadGeneratedBy
            End Get
            Set(value As PayloadSource)
                _PayloadGeneratedBy = value
            End Set
        End Property

        Private _CandleColor As Color
        Public ReadOnly Property CandleColor As Color 'Implements IPayload.CandleColor
            Get
                If Me.ClosePrice.Value > Me.OpenPrice.Value Then
                    _CandleColor = Color.Green
                ElseIf Me.ClosePrice.Value < Me.OpenPrice.Value Then
                    _CandleColor = Color.Red
                Else
                    _CandleColor = Color.White
                End If
                Return _CandleColor
            End Get
        End Property

        Private _CandleBody As Double
        Public ReadOnly Property CandleBody As Double
            Get
                _CandleBody = Math.Abs(Me.OpenPrice.Value - Me.ClosePrice.Value)
                Return _CandleBody
            End Get
        End Property

        Private _CandleRange As Decimal
        Public ReadOnly Property CandleRange As Decimal 'Implements IPayload.CandleRange
            Get
                _CandleRange = Me.HighPrice.Value - Me.LowPrice.Value
                Return _CandleRange
            End Get
        End Property

        Private _CandleRangePercentage As Decimal
        Public ReadOnly Property CandleRangePercentage As Decimal 'Implements IPayload.CandleRangePercentage
            Get
                _CandleRangePercentage = Me.CandleRange * 100 / Me.ClosePrice.Value
                Return _CandleRangePercentage
            End Get
        End Property

        Private _CandleWicks As Wicks
        Public ReadOnly Property CandleWicks As Wicks 'Implements IPayload.CandleWicks
            Get
                If _CandleWicks Is Nothing Then _CandleWicks = New Wicks
                If Me.CandleColor = Color.Green Then
                    With _CandleWicks
                        .Top = Me.HighPrice.Value - Me.ClosePrice.Value
                        .Bottom = Me.OpenPrice.Value - Me.LowPrice.Value
                    End With
                ElseIf Me.CandleColor = Color.Red Then
                    With _CandleWicks
                        .Top = Me.HighPrice.Value - Me.OpenPrice.Value
                        .Bottom = Me.ClosePrice.Value - Me.LowPrice.Value
                    End With
                Else
                    With _CandleWicks
                        .Top = Me.HighPrice.Value - Me.OpenPrice.Value
                        .Bottom = Me.ClosePrice.Value - Me.LowPrice.Value
                    End With
                End If
                Return _CandleWicks
            End Get
        End Property

        Private _DeadCandle As Double
        Public ReadOnly Property DeadCandle As Boolean
            Get
                If Me.CandleBody = 0 AndAlso Me.HighPrice.Value = Me.LowPrice.Value Then
                    _DeadCandle = True
                Else
                    _DeadCandle = False
                End If
                Return _DeadCandle
            End Get
        End Property


        Public Overrides Function ToString() As String
            Return String.Format("TradingSymbol:{0}, Open:{1}, High:{2}, Low:{3}, Close:{4}, Volume:{5}, Timestamp:{6}, Source:{7}, DailyVolume:{8}, PreviousCandleTime:{9}",
                                 Me.TradingSymbol, Me.OpenPrice.Value, Me.HighPrice.Value, Me.LowPrice.Value, Me.ClosePrice.Value, Me.Volume.Value,
                                 Me.SnapshotDateTime.ToString(), Me.PayloadGeneratedBy.ToString, Me.DailyVolume,
                                 If(Me.PreviousPayload Is Nothing, "Nothing", Me.PreviousPayload.SnapshotDateTime.ToString()))
        End Function
        Public Overrides Function Equals(obj As Object) As Boolean
            Dim compareWith As OHLCPayload = obj
            With Me
                Return .OpenPrice.Value = compareWith.OpenPrice.Value And
                .HighPrice.Value = compareWith.HighPrice.Value And
                .LowPrice.Value = compareWith.LowPrice.Value And
                .ClosePrice.Value = compareWith.ClosePrice.Value And
                .Volume.Value = compareWith.Volume.Value And
                Utilities.Time.IsTimeEqualTillSeconds(.SnapshotDateTime, compareWith.SnapshotDateTime)
            End With
        End Function
        Public Overridable Overloads Function Equals(openPrice As Decimal,
                                                     highPrice As Decimal,
                                                     lowPrice As Decimal,
                                                     closePrice As Decimal,
                                                     volume As Long,
                                                     snapshotDateTime As Date) As Boolean
            With Me
                Return .OpenPrice.Value = openPrice And
                .HighPrice.Value = highPrice And
                .LowPrice.Value = lowPrice And
                .ClosePrice.Value = closePrice And
                .Volume.Value = volume And
                Utilities.Time.IsTimeEqualTillSeconds(.SnapshotDateTime, snapshotDateTime)
            End With
        End Function
        Enum PayloadSource
            Tick
            Historical
            CalculatedTick
            CalculatedHistorical
            None
        End Enum
        Class Wicks
            Public Property Top As Double
            Public Property Bottom As Double
        End Class
    End Class
End Namespace