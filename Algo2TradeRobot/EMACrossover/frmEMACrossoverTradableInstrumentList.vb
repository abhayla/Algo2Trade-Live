Public Class frmEMACrossoverTradableInstrumentList

#Region "Common Delegate"
    Delegate Sub SetObjectEnableDisable_Delegate(ByVal [obj] As Object, ByVal [value] As Boolean)
    Public Sub SetObjectEnableDisable_ThreadSafe(ByVal [obj] As Object, ByVal [value] As Boolean)
        ' InvokeRequired required compares the thread ID of the calling thread to the thread ID of the creating thread.  
        ' If these threads are different, it returns true.  
        If [obj].InvokeRequired Then
            Dim MyDelegate As New SetObjectEnableDisable_Delegate(AddressOf SetObjectEnableDisable_ThreadSafe)
            Me.Invoke(MyDelegate, New Object() {[obj], [value]})
        Else
            [obj].Enabled = [value]
        End If
    End Sub

    Delegate Sub SetObjectText_Delegate(ByVal [Object] As Object, ByVal [text] As String)
    Public Sub SetObjectText_ThreadSafe(ByVal [Object] As Object, ByVal [text] As String)
        ' InvokeRequired required compares the thread ID of the calling thread to the thread ID of the creating thread.  
        ' If these threads are different, it returns true.  
        If [Object].InvokeRequired Then
            Dim MyDelegate As New SetObjectText_Delegate(AddressOf SetObjectText_ThreadSafe)
            Me.Invoke(MyDelegate, New Object() {[Object], [text]})
        Else
            [Object].Text = [text]
        End If
    End Sub
#End Region

    Private _TradableStrategyInstruments As IEnumerable(Of EMACrossoverStrategyInstrument)
    Private _CloseAllowed As Boolean
    Public Sub New(ByVal associatedTradableInstruments As IEnumerable(Of EMACrossoverStrategyInstrument))
        InitializeComponent()
        Me._TradableStrategyInstruments = associatedTradableInstruments
        Me._CloseAllowed = True
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
            runningColumn.ReadOnly = IIf(runningColumn.Index = 0, False, True)
        Next
    End Sub

    Private Async Sub btnExitEMACrossoverTradableInstrument_Click(sender As Object, e As EventArgs) Handles btnExitEMACrossoverTradableInstrument.Click
        SetObjectText_ThreadSafe(btnExitEMACrossoverTradableInstrument, "Waiting for Exit")
        SetObjectEnableDisable_ThreadSafe(btnExitEMACrossoverTradableInstrument, False)
        SetObjectEnableDisable_ThreadSafe(dgvTradableInstruments, False)
        _CloseAllowed = False

        Dim strategyInstrumentsToBeStopped As List(Of EMACrossoverStrategyInstrument) = Nothing
        For Each runningRow As DataGridViewRow In dgvTradableInstruments.Rows
            If Convert.ToBoolean(runningRow.Cells(0).Value) Then
                If strategyInstrumentsToBeStopped Is Nothing Then strategyInstrumentsToBeStopped = New List(Of EMACrossoverStrategyInstrument)
                strategyInstrumentsToBeStopped.Add(CType(runningRow.Cells(7).Value, EMACrossoverStrategyInstrument))
            End If
        Next
        'TODO:
        'Set relevant flags in strategy instrument collection by reading strategyInstrumentsToBeStopped
        If strategyInstrumentsToBeStopped IsNot Nothing AndAlso strategyInstrumentsToBeStopped.Count > 0 Then
            For Each runningStrategyInstrumentsToBeStopped In strategyInstrumentsToBeStopped
                runningStrategyInstrumentsToBeStopped.ForceExitByUser = True
            Next
            While True
                Dim ret As Boolean = False
                For Each runningStrategyInstrumentsToBeStopped In strategyInstrumentsToBeStopped
                    ret = ret Or runningStrategyInstrumentsToBeStopped.ForceExitByUser
                Next
                If Not ret Then
                    SetObjectText_ThreadSafe(btnExitEMACrossoverTradableInstrument, "Exit")
                    SetObjectEnableDisable_ThreadSafe(btnExitEMACrossoverTradableInstrument, True)
                    SetObjectEnableDisable_ThreadSafe(dgvTradableInstruments, True)
                    _CloseAllowed = True
                    Exit While
                End If
                Await Task.Delay(1000).ConfigureAwait(False)
            End While
        Else
            SetObjectText_ThreadSafe(btnExitEMACrossoverTradableInstrument, "Exit")
            SetObjectEnableDisable_ThreadSafe(btnExitEMACrossoverTradableInstrument, True)
            SetObjectEnableDisable_ThreadSafe(dgvTradableInstruments, True)
            _CloseAllowed = True
        End If
    End Sub

    Private Sub frmEMACrossoverTradableInstrumentList_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        If Not _CloseAllowed Then
            e.Cancel = True
        End If
    End Sub

End Class