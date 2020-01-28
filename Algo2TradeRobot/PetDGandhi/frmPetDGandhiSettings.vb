Imports System.Threading
Imports System.IO

Public Class frmPetDGandhiSettings
    Private _cts As CancellationTokenSource = Nothing
    Private _settings As PetDGandhiUserInputs = Nothing
    Private _PetDGandhiSettingsFilename As String = Path.Combine(My.Application.Info.DirectoryPath, "PetDGandhiSettings.Strategy.a2t")

    Public Sub New(ByRef PetDGandhiUserInputs As PetDGandhiUserInputs)
        InitializeComponent()
        _settings = PetDGandhiUserInputs
    End Sub

    Private Sub frmPetDGandhiSettings_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        LoadSettings()
    End Sub

    Private Sub btnSavePetDGandhiSettings_Click(sender As Object, e As EventArgs) Handles btnSavePetDGandhiSettings.Click
        Try
            _cts = New CancellationTokenSource
            If _settings Is Nothing Then _settings = New PetDGandhiUserInputs
            _settings.InstrumentsData = Nothing
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
        If File.Exists(_PetDGandhiSettingsFilename) Then
            _settings = Utilities.Strings.DeserializeToCollection(Of PetDGandhiUserInputs)(_PetDGandhiSettingsFilename)
            txtSignalTimeFrame.Text = _settings.SignalTimeFrame
            dtpckrTradeStartTime.Value = _settings.TradeStartTime
            dtpckrLastTradeEntryTime.Value = _settings.LastTradeEntryTime
            dtpckrEODExitTime.Value = _settings.EODExitTime
            txtNumberOfTradePerStock.Text = _settings.NumberOfTradePerStock
            txtStockMaxLossPerDay.Text = _settings.StockMaxLossPerDay
            txtStockMaxProfitPerDay.Text = _settings.StockMaxProfitPerDay
            txtMaxLossPerDay.Text = _settings.MaxLossPerDay
            txtMaxProfitPerDay.Text = _settings.MaxProfitPerDay
            txtInstrumentDetalis.Text = _settings.InstrumentDetailsFilePath

            chbAutoSelectStock.Checked = _settings.AutoSelectStock
            rdbCash.Checked = _settings.CashInstrument
            rdbFuture.Checked = _settings.FutureInstrument

            txtMinPrice.Text = _settings.MinPrice
            txtMaxPrice.Text = _settings.MaxPrice
            txtATRPercentage.Text = _settings.ATRPercentage
            txtMinVolume.Text = _settings.MinVolume
            txtBlankCandlePercentage.Text = _settings.BlankCandlePercentage
            txtNumberOfStock.Text = _settings.NumberOfStock
        End If
    End Sub
    Private Sub SaveSettings()
        _settings.SignalTimeFrame = txtSignalTimeFrame.Text
        _settings.TradeStartTime = dtpckrTradeStartTime.Value
        _settings.LastTradeEntryTime = dtpckrLastTradeEntryTime.Value
        _settings.EODExitTime = dtpckrEODExitTime.Value
        _settings.NumberOfTradePerStock = txtNumberOfTradePerStock.Text
        _settings.StockMaxLossPerDay = Math.Abs(CDec(txtStockMaxLossPerDay.Text)) * -1
        _settings.StockMaxProfitPerDay = txtStockMaxProfitPerDay.Text
        _settings.MaxLossPerDay = Math.Abs(CDec(txtMaxLossPerDay.Text)) * -1
        _settings.MaxProfitPerDay = txtMaxProfitPerDay.Text
        _settings.InstrumentDetailsFilePath = txtInstrumentDetalis.Text

        _settings.AutoSelectStock = chbAutoSelectStock.Checked
        _settings.CashInstrument = rdbCash.Checked
        _settings.FutureInstrument = rdbFuture.Checked

        _settings.MinPrice = txtMinPrice.Text
        _settings.MaxPrice = txtMaxPrice.Text
        _settings.ATRPercentage = txtATRPercentage.Text
        _settings.MinVolume = txtMinVolume.Text
        _settings.BlankCandlePercentage = txtBlankCandlePercentage.Text
        _settings.NumberOfStock = txtNumberOfStock.Text

        Utilities.Strings.SerializeFromCollection(Of PetDGandhiUserInputs)(_PetDGandhiSettingsFilename, _settings)
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
        _settings.FillInstrumentDetails(txtInstrumentDetalis.Text, _cts)
    End Sub
    Private Sub ValidateInputs()
        ValidateNumbers(1, 60, txtSignalTimeFrame)
        ValidateFile()
    End Sub

End Class