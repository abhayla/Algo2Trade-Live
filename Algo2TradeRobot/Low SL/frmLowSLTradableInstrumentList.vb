Public Class frmLowSLTradableInstrumentList

    Private _TradableInstruments As IEnumerable(Of LowSLStrategyInstrument)
    Public Sub New(ByVal associatedTradableInstruments As IEnumerable(Of LowSLStrategyInstrument))
        InitializeComponent()
        Me._TradableInstruments = associatedTradableInstruments
    End Sub

    Private Sub frmJoyMaaATMTradableInstrumentList_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        If _TradableInstruments IsNot Nothing AndAlso _TradableInstruments.Count > 0 Then
            Dim dt As New DataTable
            dt.Columns.Add("Instrument Name")
            dt.Columns.Add("Quantity")
            dt.Columns.Add("SL Point")
            dt.Columns.Add("Day ATR")
            dt.Columns.Add("Exchange")
            dt.Columns.Add("Instrument Type")
            dt.Columns.Add("Expiry")
            dt.Columns.Add("Lot Size")
            dt.Columns.Add("Tick Size")
            For Each instrument In _TradableInstruments
                If CType(instrument, LowSLStrategyInstrument).EligibleToTakeTrade Then
                    Dim row As DataRow = dt.NewRow
                    row("Instrument Name") = instrument.TradableInstrument.TradingSymbol
                    row("Quantity") = CType(instrument, LowSLStrategyInstrument).Quantity
                    row("SL Point") = CType(instrument, LowSLStrategyInstrument).SLPoint
                    row("Day ATR") = CType(instrument, LowSLStrategyInstrument).DayATR
                    row("Exchange") = instrument.TradableInstrument.RawExchange
                    row("Instrument Type") = instrument.TradableInstrument.RawInstrumentType
                    row("Expiry") = instrument.TradableInstrument.Expiry
                    row("Lot Size") = instrument.TradableInstrument.LotSize
                    row("Tick Size") = instrument.TradableInstrument.TickSize
                    dt.Rows.Add(row)
                End If
            Next
            dgvTradableInstruments.DataSource = dt
            dgvTradableInstruments.Refresh()
        End If
    End Sub
End Class