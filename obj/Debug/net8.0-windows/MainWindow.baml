   M S B A M L   `   `   `  ����  PumpFunSniper    PumpFunSniper.MainWindow   5   	   W SPresentationCore, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     5   	   R NSystem.Xaml, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   -   5          � Title$  PumpFun Sniper��5   	   $	��450��6    $	��800��6-   .��5      �  � RowDefinitions 5   
   �� 5      $
��Auto �6   �� 5      $
��Auto �6   �� 5	      $��* �6   .+�5   
   �� -   5         ��Name$ 
StatusText��5      $>��8Статус: Ожидание подключения...��6(   $��10q�6O   $
��Bold"�6[   $��0��6m   � 5   
   -   5         �Name$ ProgressBar��5       � IsIndeterminate$
 True��6+   $��20��6B   $��10q�6N   $��1��6Z   $y�Hidden=�6g   &   System.Windows.Controls.DataGrid  5   
   -   5          Name$ 	TokenGrid��5      $��2��6&      AutoGenerateColumns$
 True��63   $��10q�6N   ��6Z   �� *Tokens+5      5      