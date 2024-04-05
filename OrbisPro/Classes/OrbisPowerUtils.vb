Imports System.Windows.Forms

Public Class OrbisPowerUtils

    Public Structure BatteryInfo

        Private _BatteryPercentage As Single
        Private _BatteryStatus As BatteryChargeStatus
        Private _BatteryChargeRemaining As Integer
        Private _BatteryChargeUntilFull As Integer

        Public Property BatteryPercentage As Single
            Get
                Return _BatteryPercentage
            End Get
            Set
                _BatteryPercentage = Value
            End Set
        End Property

        Public Property BatteryStatus As BatteryChargeStatus
            Get
                Return _BatteryStatus
            End Get
            Set
                _BatteryStatus = Value
            End Set
        End Property

        Public Property BatteryChargeRemaining As Integer
            Get
                Return _BatteryChargeRemaining
            End Get
            Set
                _BatteryChargeRemaining = Value
            End Set
        End Property

        Public Property BatteryChargeUntilFull As Integer
            Get
                Return _BatteryChargeUntilFull
            End Get
            Set
                _BatteryChargeUntilFull = Value
            End Set
        End Property

    End Structure

    Public Shared Function IsMobileDevice() As Boolean
        Dim CurrentPowerStatus As PowerStatus = SystemInformation.PowerStatus
        Dim CurrentBatteryChargeStatus As BatteryChargeStatus = CurrentPowerStatus.BatteryChargeStatus

        If CurrentBatteryChargeStatus = BatteryChargeStatus.NoSystemBattery Then
            Return False
        Else
            Return True
        End If
    End Function

    Public Shared Function GetBatteryInfo() As BatteryInfo
        Dim CurrentPowerStatus As PowerStatus = SystemInformation.PowerStatus
        Dim CurrentBatteryChargeStatus As BatteryChargeStatus = CurrentPowerStatus.BatteryChargeStatus
        Dim NewBatteryInfo As New BatteryInfo With {
            .BatteryPercentage = CurrentPowerStatus.BatteryLifePercent,
            .BatteryStatus = CurrentBatteryChargeStatus,
            .BatteryChargeRemaining = CurrentPowerStatus.BatteryLifeRemaining,
            .BatteryChargeUntilFull = CurrentPowerStatus.BatteryFullLifetime
        }
        Return NewBatteryInfo
    End Function

    Public Shared Function GetBatteryImage(BatteryPercentageValue As Single, Optional IsCharging As Boolean = False) As ImageSource
        Dim TempBitmapImage = New BitmapImage()

        TempBitmapImage.BeginInit()
        TempBitmapImage.CacheOption = BitmapCacheOption.OnLoad
        TempBitmapImage.CreateOptions = BitmapCreateOptions.IgnoreImageCache

        Dim IntPercentage As Integer = CInt(BatteryPercentageValue)

        If IntPercentage = 100 AndAlso IsCharging Then
            TempBitmapImage.UriSource = New Uri("pack://application:,,,/Icons/Battery/BatteryFullCharging.png", UriKind.RelativeOrAbsolute)
        ElseIf IntPercentage = 100 Then
            TempBitmapImage.UriSource = New Uri("pack://application:,,,/Icons/Battery/BatteryFull.png", UriKind.RelativeOrAbsolute)
        ElseIf IntPercentage >= 80 Then
            TempBitmapImage.UriSource = New Uri("pack://application:,,,/Icons/Battery/Battery80.png", UriKind.RelativeOrAbsolute)
        ElseIf IntPercentage >= 60 Then
            TempBitmapImage.UriSource = New Uri("pack://application:,,,/Icons/Battery/Battery80.png", UriKind.RelativeOrAbsolute)
        ElseIf IntPercentage >= 40 Then
            TempBitmapImage.UriSource = New Uri("pack://application:,,,/Icons/Battery/Battery80.png", UriKind.RelativeOrAbsolute)
        ElseIf IntPercentage >= 20 Then
            TempBitmapImage.UriSource = New Uri("pack://application:,,,/Icons/Battery/Battery80.png", UriKind.RelativeOrAbsolute)
        ElseIf IntPercentage >= 1 Then
            TempBitmapImage.UriSource = New Uri("pack://application:,,,/Icons/Battery/BatteryCritical.png", UriKind.RelativeOrAbsolute)
        End If

        TempBitmapImage.EndInit()

        Return TempBitmapImage
    End Function

End Class
