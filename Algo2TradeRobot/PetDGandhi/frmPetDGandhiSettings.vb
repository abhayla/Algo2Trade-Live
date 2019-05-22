Imports System.Threading
Imports System.IO

Public Class frmPetDGandhiSettings
    Private _cts As CancellationTokenSource = Nothing
    Private _PetDGandhiSettings As PetDGandhiStrategyUserInputs = Nothing
    Private _PetDGandhiSettingsFilename As String = Path.Combine(My.Application.Info.DirectoryPath, "PetDGandhiSettings.Strategy.a2t")

    Public Sub New(ByRef PetDGandhiUserInputs As PetDGandhiStrategyUserInputs)
        InitializeComponent()
        _PetDGandhiSettings = PetDGandhiUserInputs
    End Sub

    Private Sub frmPetDGandhiSettings_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        LoadSettings()
    End Sub

    Private Sub btnSavePetDGandhiSettings_Click(sender As Object, e As EventArgs) Handles btnSavePetDGandhiSettings.Click
        Try
            _cts = New CancellationTokenSource
            If _PetDGandhiSettings Is Nothing Then _PetDGandhiSettings = New PetDGandhiStrategyUserInputs
            _PetDGandhiSettings.InstrumentsData = Nothing
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
            _PetDGandhiSettings = Utilities.Strings.DeserializeToCollection(Of PetDGandhiStrategyUserInputs)(_PetDGandhiSettingsFilename)
            txtSignalTimeFrame.Text = _PetDGandhiSettings.SignalTimeFrame
            dtpckrTradeStartTime.Value = _PetDGandhiSettings.TradeStartTime
            dtpckrLastTradeEntryTime.Value = _PetDGandhiSettings.LastTradeEntryTime
            dtpckrEODExitTime.Value = _PetDGandhiSettings.EODExitTime
            txtMaxLossPerDay.Text = _PetDGandhiSettings.MaxLossPerDay
            txtMaxProfitPerDay.Text = _PetDGandhiSettings.MaxProfitPerDay
            txtEMAPeriod.Text = _PetDGandhiSettings.EMAPeriod
            chkboxPivotHighLowStrict.Checked = _PetDGandhiSettings.PivotHighLowStrict
            txtTelegramAPI.Text = _PetDGandhiSettings.TelegramAPIKey
            txtTelegramChatID.Text = _PetDGandhiSettings.TelegramChatID
            txtTelegramChatIDForPL.Text = _PetDGandhiSettings.TelegramPLChatID
            txtInstrumentDetalis.Text = _PetDGandhiSettings.InstrumentDetailsFilePath
        End If
    End Sub
    Private Sub SaveSettings()
        _PetDGandhiSettings.SignalTimeFrame = txtSignalTimeFrame.Text
        _PetDGandhiSettings.TradeStartTime = dtpckrTradeStartTime.Value
        _PetDGandhiSettings.LastTradeEntryTime = dtpckrLastTradeEntryTime.Value
        _PetDGandhiSettings.EODExitTime = dtpckrEODExitTime.Value
        _PetDGandhiSettings.MaxLossPerDay = txtMaxLossPerDay.Text
        _PetDGandhiSettings.MaxProfitPerDay = txtMaxProfitPerDay.Text
        _PetDGandhiSettings.EMAPeriod = txtEMAPeriod.Text
        _PetDGandhiSettings.PivotHighLowStrict = chkboxPivotHighLowStrict.Checked
        _PetDGandhiSettings.TelegramAPIKey = txtTelegramAPI.Text
        _PetDGandhiSettings.TelegramChatID = txtTelegramChatID.Text
        _PetDGandhiSettings.TelegramPLChatID = txtTelegramChatIDForPL.Text
        _PetDGandhiSettings.InstrumentDetailsFilePath = txtInstrumentDetalis.Text

        Utilities.Strings.SerializeFromCollection(Of PetDGandhiStrategyUserInputs)(_PetDGandhiSettingsFilename, _PetDGandhiSettings)
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
        _PetDGandhiSettings.FillInstrumentDetails(txtInstrumentDetalis.Text, _cts)
    End Sub
    Private Sub ValidateInputs()
        ValidateNumbers(1, 60, txtSignalTimeFrame)
        ValidateNumbers(Decimal.MinValue, Decimal.MaxValue, txtMaxLossPerDay)
        ValidateNumbers(0, Decimal.MaxValue, txtMaxProfitPerDay)
        ValidateNumbers(1, Integer.MaxValue, txtEMAPeriod)
        ValidateFile()
    End Sub

End Class