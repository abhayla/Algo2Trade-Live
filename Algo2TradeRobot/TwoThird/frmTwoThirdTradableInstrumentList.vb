Public Class frmTwoThirdTradableInstrumentList

    Private _TradableInstruments As IEnumerable(Of TwoThirdStrategyInstrument)
    Public Sub New(ByVal associatedTradableInstruments As IEnumerable(Of TwoThirdStrategyInstrument))
        InitializeComponent()
        Me._TradableInstruments = associatedTradableInstruments
    End Sub

    Private Sub frmTwoThirdTradableInstrumentList_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        If _TradableInstruments IsNot Nothing AndAlso _TradableInstruments.Count > 0 Then
            Dim dt As New DataTable
            dt.Columns.Add("Instrument Name")
            dt.Columns.Add("Exchange")
            dt.Columns.Add("Instrument Type")
            dt.Columns.Add("Expiry")
            dt.Columns.Add("Lot Size")
            dt.Columns.Add("Tick Size")
            For Each instrument In _TradableInstruments
                Dim row As DataRow = dt.NewRow
                row("Instrument Name") = instrument.TradableInstrument.TradingSymbol
                row("Exchange") = instrument.TradableInstrument.RawExchange
                row("Instrument Type") = instrument.TradableInstrument.RawInstrumentType
                row("Expiry") = instrument.TradableInstrument.Expiry
                row("Lot Size") = instrument.TradableInstrument.LotSize
                row("Tick Size") = instrument.TradableInstrument.TickSize
                dt.Rows.Add(row)
            Next
            dgvTradableInstruments.DataSource = dt
            dgvTradableInstruments.Refresh()
        End If
    End Sub
End Class