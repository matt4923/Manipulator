   M S B A M L   `   `   `  ����F  BManipulator, Version=1.0.0.3, Culture=neutral, PublicKeyToken=null)clr-namespace:ManipulatorManipulator  5   	       Manipulator.MainWindow   5   	   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   +localclr-namespace:Manipulator     5   	   -   5          � Title$  Manipulator��5   	   $	��350��6   $	��525��6*   $	��350��66   $	��525��6F    � 	Resources 5	       �� 
TargetType �� Property �� Value��%W   '��       �� 5
   
   " ��6   .�5      ��  � ��$ 3,3,3,0q�6'   5      5      .��5      �  � RowDefinitions 5   
   �� 5      $
��auto �6   6,   �� 5      $��* �6   6)   �� 5      $
��auto �6   6,   �� 5      $
��auto �6   6,   .+�5   
   ]� .+�5      �� .+�5      o� -   5        $��menuMain��5   '   $��0��6   $
��Left=�67   .@�5      m� $��_File��6   .@�5      m� -   5        $��_Save Scripts As .SCP��5   #   6f   m� 5      -   5        $��_Load Scripts .SCP��5   #   6c   m� 5      -   5        $��_Version��5   #   6K   m� 5      -   5        $��_Exit��5   #   6O   5      5      �� 5       �� 6   .�6"   	Format:6+    �� 62   .�69   QOField_Name:Value,Append Location(b,e,o),Condition1=value1&Condition2=value2...;6�   6�   5!      5#      �� 5$   
   -   5        $��	txtScript��5$       �� TextWrapping$
 Wrap=�6#    �� AcceptsReturn . " �� VerticalScrollBarVisibility$
 Auto=�6L   $��1��6o   6|   ]� 5%   
   ��. $��2��6)   $��Stretch=�66   .+�5&      �� -   5        $��btnGo��5&   x   $��Right=�6   
���/���$
��Left=�6E   $	��Top=�6`   $��54��6�   .��6�   Go5(      �� 5)      -	   5        $��	btnBrowse��5)   ^   $
��Left=�6   $
��Left=�6+   $	��Top=�6F   $	��105��6�   .��6�   Get Switch File5+      �� 5,      -
   5        $��txtPath��5,      $��3q�6%   $
��Left=�60   6H   5-      5.      5/      