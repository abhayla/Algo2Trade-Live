Imports System.IO
Imports System.Threading

Public Class frmEMA_SupertrendSettings

    Private _cts As CancellationTokenSource = Nothing
    Private _EMA_SupertrendSettings As EMA_SupertrendStrategyUserInputs = Nothing
    Private _EMA_SupertrendSettingsFilename As String = Path.Combine(My.Application.Info.DirectoryPath, "EMA_SupertrendSettings.Strategy.a2t")

    Public Sub New(ByRef EMA_SupertrendUserInputs As EMA_SupertrendStrategyUserInputs)
        InitializeComponent()
        _EMA_SupertrendSettings = EMA_SupertrendUserInputs
    End Sub

    Private Sub frmEMA_SupertrendSettings_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        LoadSettings()
    End Sub

    Private Sub btnSaveEMA_SupertrendSettings_Click(sender As Object, e As EventArgs) Handles btnSaveEMA_SupertrendSettings.Click
        Try
            _cts = New CancellationTokenSource
            If _EMA_SupertrendSettings Is Nothing Then _EMA_SupertrendSettings = New EMA_SupertrendStrategyUserInputs
            _EMA_SupertrendSettings.InstrumentsData = Nothing
            ValidateInputs()
            SaveSettings()
            Me.Close()
        Catch ex As Exception
            MsgBox(String.Format("The following error occurred: {0}", ex.Message), MsgBoxStyle.Critical)
        End Try
    End Sub

    Private Sub LoadSettings()
        If File.Exists(_EMA_SupertrendSettingsFilename) Then
            _EMA_SupertrendSettings = Utilities.Strings.DeserializeToCollection(Of EMA_SupertrendStrategyUserInputs)(_EMA_SupertrendSettingsFilename)
            txtSignalTimeFrame.Text = _EMA_SupertrendSettings.SignalTimeFrame
            dtpckrTradeStartTime.Value = _EMA_SupertrendSettings.TradeStartTime
            dtpckrLastTradeEntryTime.Value = _EMA_SupertrendSettings.LastTradeEntryTime
            dtpckrEODExitTime.Value = _EMA_SupertrendSettings.EODExitTime
            txtMaxLossPercentagePerDay.Text = _EMA_SupertrendSettings.MaxLossPercentagePerDay
            txtMaxProfitPercentagePerDay.Text = _EMA_SupertrendSettings.MaxProfitPercentagePerDay
            txtFastEMAPeriod.Text = _EMA_SupertrendSettings.FastEMAPeriod
            txtSlowEMAPeriod.Text = _EMA_SupertrendSettings.SlowEMAPeriod
            txtSupertrendPeriod.Text = _EMA_SupertrendSettings.SupertrendPeriod
            txtSupertrendMultiplier.Text = _EMA_SupertrendSettings.SupertrendMultiplier
            txtInstrumentDetalis.Text = _EMA_SupertrendSettings.InstrumentDetailsFilePath
        End If
    End Sub
    Private Sub SaveSettings()
        _EMA_SupertrendSettings.SignalTimeFrame = txtSignalTimeFrame.Text
        _EMA_SupertrendSettings.TradeStartTime = dtpckrTradeStartTime.Value
        _EMA_SupertrendSettings.LastTradeEntryTime = dtpckrLastTradeEntryTime.Value
        _EMA_SupertrendSettings.EODExitTime = dtpckrEODExitTime.Value
        _EMA_SupertrendSettings.MaxLossPercentagePerDay = txtMaxLossPercentagePerDay.Text
        _EMA_SupertrendSettings.MaxProfitPercentagePerDay = txtMaxProfitPercentagePerDay.Text
        _EMA_SupertrendSettings.FastEMAPeriod = txtFastEMAPeriod.Text
        _EMA_SupertrendSettings.SlowEMAPeriod = txtSlowEMAPeriod.Text
        _EMA_SupertrendSettings.SupertrendPeriod = txtSupertrendPeriod.Text
        _EMA_SupertrendSettings.SupertrendMultiplier = txtSupertrendMultiplier.Text
        _EMA_SupertrendSettings.InstrumentDetailsFilePath = txtInstrumentDetalis.Text

        Utilities.Strings.SerializeFromCollection(Of EMA_SupertrendStrategyUserInputs)(_EMA_SupertrendSettingsFilename, _EMA_SupertrendSettings)
    End Sub
    Private Function ValidateNumbers(ByVal startNumber As Decimal, ByVal endNumber As Decimal, ByVal inputTB As TextBox) As Boolean
        Dim ret As Boolean = False
        If IsNumeric(inputTB.Text) Then
            If Val(inputTB.Text) >= startNumber And Val(inputTB.Text) <= endNumber Then
                ret = True
            End If
        End If
        If Not ret Then Throw New ApplicationException(String.Format("{0} cannot have a value < {1} or > {2}", inputTB.Tag, startNumber, endNumber))
        Return ret
    End Function
    Private Sub ValidateFile()
        _EMA_SupertrendSettings.FillInstrumentDetails(txtInstrumentDetalis.Text, _cts)
    End Sub
    Private Sub ValidateInputs()
        ValidateNumbers(1, 60, txtSignalTimeFrame)
        ValidateNumbers(0, 100, txtMaxLossPercentagePerDay)
        ValidateNumbers(0, 100, txtMaxProfitPercentagePerDay)
        ValidateNumbers(1, Integer.MaxValue, txtSlowEMAPeriod)
        ValidateNumbers(1, Integer.MaxValue, txtFastEMAPeriod)
        ValidateNumbers(1, Integer.MaxValue, txtSupertrendPeriod)
        ValidateNumbers(0.1, Decimal.MaxValue, txtSupertrendMultiplier)
        ValidateFile()
    End Sub

    Private Sub btnBrowse_Click(sender As Object, e As EventArgs) Handles btnBrowse.Click
        opnFileSettings.ShowDialog()
    End Sub

    Private Sub opnFileSettings_FileOk(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles opnFileSettings.FileOk
        Dim extension As String = Path.GetExtension(opnFileSettings.FileName)
        If extension = ".csv" Then
            txtInstrumentDetalis.Text = opnFileSettings.FileName
        Else
            MsgBox("File Type not supported. Please Try again.", MsgBoxStyle.Critical)
        End If
    End Sub
End Class