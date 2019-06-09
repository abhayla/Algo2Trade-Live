Public Class frmEMACrossoverTradableInstrumentList

    Private _TradableStrategyInstruments As IEnumerable(Of EMACrossoverStrategyInstrument)
    Public Sub New(ByVal associatedTradableInstruments As IEnumerable(Of EMACrossoverStrategyInstrument))
        InitializeComponent()
        Me._TradableStrategyInstruments = associatedTradableInstruments
    End Sub
    Private Sub frmEMACrossoverTradableInstrumentList_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        If _TradableStrategyInstruments IsNot Nothing AndAlso _TradableStrategyInstruments.Count > 0 Then
            Dim dt As New DataTable
            dt.Columns.Add("Exit", GetType(Boolean))
            dt.Columns.Add("Instrument Name")
            dt.Columns.Add("Exchange")
            dt.Columns.Add("Instrument Type")
            dt.Columns.Add("Expiry")
            dt.Columns.Add("Lot Size")
            dt.Columns.Add("Tick Size")
            dt.Columns.Add("StrategyInstrument", GetType(EMACrossoverStrategyInstrument))
            For Each instrument In _TradableStrategyInstruments
                Dim row As DataRow = dt.NewRow
                row("Exit") = False
                row("Instrument Name") = instrument.TradableInstrument.TradingSymbol
                row("Exchange") = instrument.TradableInstrument.RawExchange
                row("Instrument Type") = instrument.TradableInstrument.RawInstrumentType
                row("Expiry") = instrument.TradableInstrument.Expiry
                row("Lot Size") = instrument.TradableInstrument.LotSize
                row("Tick Size") = instrument.TradableInstrument.TickSize
                row("StrategyInstrument") = instrument
                dt.Rows.Add(row)
            Next
            dgvTradableInstruments.DataSource = dt
            dgvTradableInstruments.Columns.Item("StrategyInstrument").Visible = False
            dgvTradableInstruments.Refresh()
        End If
        For Each runningColumn In dgvTradableInstruments.Columns
            runningColumn.ReadOnly = IIf(runningColumn.Index = 1, True, False)
        Next
    End Sub

    Private Sub btnExitEMACrossoverTradableInstrument_Click(sender As Object, e As EventArgs) Handles btnExitEMACrossoverTradableInstrument.Click
        Dim strategyInstrumentsToBeStopped As List(Of EMACrossoverStrategyInstrument) = Nothing
        For Each runningRow As DataGridViewRow In dgvTradableInstruments.Rows
            If Convert.ToBoolean(runningRow.Cells(0).Value) Then
                If strategyInstrumentsToBeStopped Is Nothing Then strategyInstrumentsToBeStopped = New List(Of EMACrossoverStrategyInstrument)
                strategyInstrumentsToBeStopped.Add(CType(runningRow.Cells(7).Value, EMACrossoverStrategyInstrument))
            End If
        Next
        'TODO:
        'Set relevant flags in strategy instrument collection by reading strategyInstrumentsToBeStopped
    End Sub
End Class