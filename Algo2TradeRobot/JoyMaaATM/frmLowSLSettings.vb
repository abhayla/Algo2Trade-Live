Imports System.Threading
Imports System.IO

Public Class frmLowSLSettings
    Private _cts As CancellationTokenSource = Nothing
    Private _JoyMaaATMSettings As LowSLUserInputs = Nothing
    Private _JoyMaaATMSettingsFilename As String = Path.Combine(My.Application.Info.DirectoryPath, "LowSLSettings.Strategy.a2t")

    Public Sub New(ByRef userInputs As LowSLUserInputs)
        InitializeComponent()
        _JoyMaaATMSettings = userInputs
    End Sub

    Private Sub frmJoyMaaATMSettings_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        LoadSettings()
    End Sub

    Private Sub btnJoyMaaATMSettings_Click(sender As Object, e As EventArgs) Handles btnJoyMaaATMStrategySettings.Click
        Try
            _cts = New CancellationTokenSource
            If _JoyMaaATMSettings Is Nothing Then _JoyMaaATMSettings = New LowSLUserInputs
            _JoyMaaATMSettings.InstrumentsData = Nothing
            ValidateInputs()
            SaveSettings()
            Me.Close()
        Catch ex As Exception
            MsgBox(String.Format("The following error occurred: {0}", ex.Message), MsgBoxStyle.Critical)
        End Try
    End Sub

    Private Sub btnBrowse_Click(sender As Object, e As EventArgs) Handles btnBrowse.Click
        opnFileSettings.Filter = "|*.csv"
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

    Private Sub LoadSettings()
        If File.Exists(_JoyMaaATMSettingsFilename) Then
            _JoyMaaATMSettings = Utilities.Strings.DeserializeToCollection(Of LowSLUserInputs)(_JoyMaaATMSettingsFilename)
            txtATRPeriod.Text = _JoyMaaATMSettings.ATRPeriod
            txtSignalTimeFrame.Text = _JoyMaaATMSettings.SignalTimeFrame
            dtpckrTradeStartTime.Value = _JoyMaaATMSettings.TradeStartTime
            dtpckrLastTradeEntryTime.Value = _JoyMaaATMSettings.LastTradeEntryTime
            dtpckrEODExitTime.Value = _JoyMaaATMSettings.EODExitTime
            txtTargetMultiplier.Text = _JoyMaaATMSettings.TargetMultiplier
            txtInstrumentDetalis.Text = _JoyMaaATMSettings.InstrumentDetailsFilePath
            chbAutoSelectStock.Checked = _JoyMaaATMSettings.AutoSelectStock
            chbCash.Checked = _JoyMaaATMSettings.CashInstrument
            chbFuture.Checked = _JoyMaaATMSettings.FutureInstrument
            txtCashMaxSL.Text = _JoyMaaATMSettings.CashMaxSL
            txtFutureMinCapital.Text = _JoyMaaATMSettings.FutureMinCapital
            txtManualStockList.Text = _JoyMaaATMSettings.ManualInstrumentList
            txtMinPrice.Text = _JoyMaaATMSettings.MinPrice
            txtMaxPrice.Text = _JoyMaaATMSettings.MaxPrice
            txtATRPercentage.Text = _JoyMaaATMSettings.ATRPercentage
            txtMinVolume.Text = _JoyMaaATMSettings.MinVolume
            txtNumberOfStock.Text = _JoyMaaATMSettings.NumberOfStock
            txtTelegramAPI.Text = _JoyMaaATMSettings.TelegramAPIKey
            txtTelegramChatID.Text = _JoyMaaATMSettings.TelegramChatID
            txtTelegramChatIDForPL.Text = _JoyMaaATMSettings.TelegramPLChatID
        End If
    End Sub

    Private Sub SaveSettings()
        _JoyMaaATMSettings.ATRPeriod = txtATRPeriod.Text
        _JoyMaaATMSettings.SignalTimeFrame = txtSignalTimeFrame.Text
        _JoyMaaATMSettings.TradeStartTime = dtpckrTradeStartTime.Value
        _JoyMaaATMSettings.LastTradeEntryTime = dtpckrLastTradeEntryTime.Value
        _JoyMaaATMSettings.EODExitTime = dtpckrEODExitTime.Value
        _JoyMaaATMSettings.TargetMultiplier = txtTargetMultiplier.Text
        _JoyMaaATMSettings.InstrumentDetailsFilePath = txtInstrumentDetalis.Text
        _JoyMaaATMSettings.AutoSelectStock = chbAutoSelectStock.Checked
        _JoyMaaATMSettings.CashInstrument = chbCash.Checked
        _JoyMaaATMSettings.FutureInstrument = chbFuture.Checked
        _JoyMaaATMSettings.CashMaxSL = txtCashMaxSL.Text
        _JoyMaaATMSettings.FutureMinCapital = txtFutureMinCapital.Text
        _JoyMaaATMSettings.ManualInstrumentList = txtManualStockList.Text
        _JoyMaaATMSettings.MinPrice = txtMinPrice.Text
        _JoyMaaATMSettings.MaxPrice = txtMaxPrice.Text
        _JoyMaaATMSettings.ATRPercentage = txtATRPercentage.Text
        _JoyMaaATMSettings.MinVolume = txtMinVolume.Text
        _JoyMaaATMSettings.NumberOfStock = txtNumberOfStock.Text
        _JoyMaaATMSettings.TelegramAPIKey = txtTelegramAPI.Text
        _JoyMaaATMSettings.TelegramChatID = txtTelegramChatID.Text
        _JoyMaaATMSettings.TelegramPLChatID = txtTelegramChatIDForPL.Text

        Utilities.Strings.SerializeFromCollection(Of LowSLUserInputs)(_JoyMaaATMSettingsFilename, _JoyMaaATMSettings)
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
        _JoyMaaATMSettings.FillInstrumentDetails(txtInstrumentDetalis.Text, _cts)
    End Sub

    Private Sub ValidateInputs()
        ValidateNumbers(1, 60, txtSignalTimeFrame)
        ValidateNumbers(1, 100, txtATRPeriod)
        ValidateNumbers(0, Decimal.MaxValue, txtCashMaxSL)
        ValidateNumbers(0, Decimal.MaxValue, txtFutureMinCapital)
        ValidateFile()
    End Sub
End Class