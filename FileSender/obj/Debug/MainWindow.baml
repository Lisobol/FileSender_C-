   M S B A M L   `   `   `  ����  
FileSender    FileSender.MainWindow   5   	   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   -   5          � Title$=  7Программа для передачи файлов��5   	   $	��324��6/   $	��469��6<    � WindowStyle$ SingleBorderWindow=�6v    � 
ResizeMode$ NoResize=�6�   ���	#FF688089.��5      � $
��Auto��6   $
��Auto��6   $��0,0,0,-1q�6&   ��5   
   �� 5       �� ImageSource$ 	back2.jpg��6    � ColumnDefinitions 5	   
   �� 5
      $
��161* �6   �� 5      $
��149* �6   �� 5      $
��153* �6   .+�5   
   �� -   5        $��	labelConn��5      $��Подключено��6   ���	#FFDDE4E3
����������	#FF616566$��0,0,51.132,20q�6h   $��Right=�6�   $	��100��6�   $��30��6�   $��Bottom=�6�   �� 5   
   -   5        $��
labelYesNo��5   �   $��Нет��6   ���	#FFDFE8E7
����������	#FF616566$��30��6a   $��
0,0,115,20q�6m   $��Bottom=�6�   $��Right=�6�   $��85��6�   $��2��6�   �� 5   
   -   5        $��	labelKurs��5   �   $=��7Программа для передачи файлов��6   ���	#FFC6E4E1 J� Opacity$	 0.7Q�6O   ���	#FF34383A$��42��6t   $��Stretch=�6�   $	��Top=�6�   $��16(�6�   $��Normal"�6�   {�. $��Center=�6�   $��0,32,0,0q�6  $��3��60  �� 5   
   -   5         ��Name$ 
buttonSend��5   e   $!��Отправить файл��6   $��29��6*   $
��Left=�66   $��
10,245,0,0q�6Q   $	��Top=�6y   $	��111��6�   $��2��6�   � 5   
   -   5        $��progressBar1��5   Q   $��20��6   $
��Left=�6"   $��
51,191,0,0q�6=   $	��Top=�6e   $	��219��6}   $y�Hidden=�6�   $��2��6�   $��1��6�   o� 5   
   -   5        $
��Menu��5   D   $��27��6   $��Stretch=�6   $��0q�69   $	��Top=�6P   $
��Auto��6h   $��3��6u   ���	#FFB2B6B8.@�5      m� -   5         m�Name$ MenuItem_Help��5   (   $��Справка��6   $��66��6?   
���   ����	#FFB2B6B8.@�5      m� -	   5        $ MenuItem_About��5   0   $��О программе��6   
���   ����	#FFB2B6B86w   m� 5      -
   5        $ MenuItem_Stud��5   1   $��Разработчики��6   
���   ����	#FFB2B6B86v   5      5      �� 5   
   ���	#FF616566$
��Left=�6%   $��23��6@   $��	10,89,0,0q�6L   $	��Top=�6_   $	��170��6w   $��2��6�   9� 5   
   -   5         9�Name$ ComPortSettings��5      $'��!Настройки COM-порта��6,   $
��Left=�6I   $	��140��6d   $��	10,89,0,0q�6q   $	��Top=�6�   $	��170��6�   $��2��6�   ���	#FFE8EEF0.��5      �� $
��Left=�6   $	��Top=�60   .+�5      � ���	#FF1022296   �� 5      -   5        	 ��Name$	 	PortsList��5      $��23��6/   $��70��6;   $��80,5,0,0q�6F   5      �� 5      -   5        
 ��Name$
 	PortSpeed��5      $��23��6.   $��70��6:   $��	80,30,0,0q�6E   �� 5       $��№ порта��6   $	��Top=�6*   $��25��6B   $��10,3,0,0q�6N   ���	#FF616566���	#FFE8EEF06w   �� 5!      $��Скорость��6   $	��Top=�6+   $��23��6C   $��	10,30,0,0q�6O   ���	#FF616566���	#FFE8EEF06y   �� 5"      -   5        $ ApplyButton��5"      $��	80,55,0,0q�6.   $��70��6A   $��Применить��6L   ���	#FFACA6A66�   5#      5$      �� 5%   
   -   5         ��Name$ label4��5%   ~   $*��$Идет передача файла��6   $y�Hidden=�6.   $��37��6C   $
��Left=�6O   $��
51,149,0,0q�6j   $	��Top=�6�   $	��219��6�   $��18(�6�   $��2��6�   ���	#FF595555���	#FFF5F1F1 J� IsHitTestVisible . $��1��6  5&      5'      