using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.IO.Ports;
using System.Threading;
using Microsoft.Win32;
namespace FileSender
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        SerialPort ComPort;
        bool Connection = false;
        SynchronizationContext SynchWindow;//контекст синхронизации для обращения из одного потока к элементам другого
        FileStream ReadFileStream;//файловый поток для чтения;
        FileStream WriteFileStream;//файловый поток для записи;      
        string SourcePath;
        string PurposePath;
        long Pos;                 //последняя записываемая позиция
        bool HeaderSent = false;      //отправлен заголовок
        bool InformSent = false;      //отправка иноформационной части
        bool HeaderRec = false;      //получен заголовок
        bool InformRec = false;      //получение информационной части
        bool Confirmation = false;     //необходимо подтверждение
        Thread ThreadRead;// объект, описывающий параллельный поток чтения данных из буфера COM-порта
        Thread ThreadCheckConn;// объект, описывающий параллельный поток проверки соединения;
        int ErrCount = 0;

        void HeadInfSentRecFALSE()
        {
            HeaderRec = false;
            InformRec = false;
            HeaderSent = false;
            InformSent = false;
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            buttonSend.IsEnabled = false;
            SynchWindow = SynchronizationContext.Current;
            ThreadRead = new Thread(FrameReceiving);
            ThreadCheckConn = new Thread(Connect);
            string[] ArrayOfPorts;
            ArrayOfPorts = SerialPort.GetPortNames();
            foreach (string port in ArrayOfPorts)
            {
                var NewItem = new ComboBoxItem();
                NewItem.Content = port;
                SynchWindow.Send(d => PortsList.Items.Add(NewItem), null);
            }
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            var SelectedPort = PortsList.Text;
            var Speed = 0;
            var WrongFormat = false;
            try
            {
                Speed = Convert.ToInt16(PortSpeed.Text);
            }
            catch (Exception)
            {
                MessageBox.Show("Неверный формат или слишком большое число! Введите число");
                WrongFormat = true;
            }
            if (!WrongFormat)
            {
                try
                {
                    ComPort = new SerialPort(SelectedPort, Speed, Parity.None, 8, StopBits.One);
                    ComPort.Handshake = Handshake.None;
                    ComPort.DtrEnable = true;//сигн готовности dtr терминала
                    ComPort.RtsEnable = false;//сигнзапроса передачи
                    ComPort.ReadTimeout = 500;
                    ComPort.WriteTimeout = 500;
                    ComPort.Open();
                    ThreadCheckConn.Start();
                    ThreadRead.Start();
                }
                catch (InvalidOperationException) { }
                catch (UnauthorizedAccessException) { MessageBox.Show("Данный порт уже занят"); }
                catch (Exception) { MessageBox.Show("Выберите порт!"); }
                try
                {
                    MessageBox.Show("Выбран порт " + ComPort.PortName + " и задана скорость передачи " + Convert.ToString(Speed));
                    SynchWindow.Send(d => ApplyButton.IsEnabled = false, null);
                }
                catch (Exception) { }

            }

        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Connection = false;
            ThreadRead.Abort();
            ThreadCheckConn.Abort();
            try
            {
                ComPort.Close();
            }
            catch (Exception) { };
        }

        private void buttonSend_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog File = new OpenFileDialog();
            File.Multiselect = false;
            File.Title = "Выберите передаваемый файл";
            bool? ChosenFile = File.ShowDialog();
            string FileName;
            if (ChosenFile == true)
            {
                SourcePath = File.FileName;
                ReadFileStream = new FileStream(SourcePath, FileMode.Open, FileAccess.Read);
                progressBar1.Maximum = (ReadFileStream.Length / 40) + 1;
                progressBar1.Value = 0;
                FileName = SourcePath.Substring(SourcePath.LastIndexOf('\\') + 1);
                var FileLen = ReadFileStream.Length;
                byte[] Header = new byte[FileName.Count()];
                for (int i = 0; i < FileName.Count(); i++)
                {
                    Header[i] = Convert.ToByte(FileName[i]);
                }
                HEADER(Header, FileLen);
            }
        }

        public void Connect()
        {
            while (true)
            {
                if ((ComPort.DsrHolding == true) & (HeaderRec == false) & (HeaderSent == false))//DSR-datasetready активен. Передача не идет
                {
                    Connection = true;
                    SynchWindow.Send(d => labelYesNo.Content = "Есть", null);
                    SynchWindow.Send(d => buttonSend.IsEnabled = true, null);
                }
                if ((ComPort.DsrHolding == false) & (HeaderRec == false) & (HeaderSent == false))//DSR-не активен. Передача не идет
                {
                    Connection = false;
                    SynchWindow.Send(d => labelYesNo.Content = "Нет", null);
                    SynchWindow.Send(d => buttonSend.IsEnabled = false, null);
                }
                if (((HeaderRec == true) || (HeaderSent == true)) & (Connection == false))//идет передача, нет соединения=>ошибка+закрытие потоков
                {
                    SynchWindow.Send(d => progressBar1.Visibility = Visibility.Hidden, null);
                    SynchWindow.Send(d => label4.Visibility = Visibility.Hidden, null);
                    SynchWindow.Send(d => buttonSend.IsEnabled = true, null);
                    MessageBox.Show("Ошибка передачи");
                    HeadInfSentRecFALSE();
                    WriteFileStream.Close();
                    WriteFileStream.Dispose();
                    ReadFileStream.Close();
                    ReadFileStream.Dispose();
                }
                Thread.Sleep(1000);
            }
        }

        void HEADER(byte[] Header, long FileLen)
        {
            byte[] header = CodeHemming(FrameForming(Header, 'H', Header.Count(), FileLen));
            ComPort.RtsEnable = false;
            Confirmation = true;
            ComPort.Write(header, 0, header.Count());//byte []buffer,смещение,кол-во байт
            ComPort.RtsEnable = true;
        }
        void INFO(byte[] inf)
        {
            byte[] frame = CodeHemming(FrameForming(inf, 'I', inf.Count()));
            ComPort.RtsEnable = false;
            Confirmation = true;
            ComPort.Write(frame, 0, frame.Count());//запись кадра в буфер COM
            ComPort.RtsEnable = true;
        }
        void ACK()
        {
            ComPort.RtsEnable = false;
            ComPort.Write(CodeHemming(FrameForming('A')), 0, CodeHemming(FrameForming('A')).Count());
            ComPort.RtsEnable = true;
        }
        void NAK()
        {
            ComPort.RtsEnable = false;
            ComPort.Write(CodeHemming(FrameForming('N')), 0, CodeHemming(FrameForming('N')).Count());
            ComPort.RtsEnable = true;
        }
        void TRUE()
        {
            ComPort.RtsEnable = false;
            ComPort.Write(CodeHemming(FrameForming('T')), 0, CodeHemming(FrameForming('T')).Count());
            ComPort.RtsEnable = true;
        }
        void FALSE()
        {
            ComPort.RtsEnable = false;
            ComPort.Write(CodeHemming(FrameForming('F')), 0, CodeHemming(FrameForming('F')).Count());
            ComPort.RtsEnable = true;
        }
        void END()
        {
            ComPort.RtsEnable = false;
            ComPort.Write(CodeHemming(FrameForming('E')), 0, CodeHemming(FrameForming('E')).Count());
            ComPort.RtsEnable = true;
        }

        byte[] FrameForming(char Type)
        {
            byte[] Byte;
            if (Type == 'A' || Type == 'T' || Type == 'N' || Type == 'F' || Type == 'E')
            {
                Byte = new byte[3];
                Byte[0] = byte.Parse("FF", System.Globalization.NumberStyles.AllowHexSpecifier);
                Byte[1] = Convert.ToByte(Type);
                Byte[2] = byte.Parse("FF", System.Globalization.NumberStyles.AllowHexSpecifier);
            }
            else
            {
                Byte = new byte[0];
            }
            return Byte;
        }

        byte[] FrameForming(byte[] InfByte, char Type, long Length)
        {
            byte[] Byte;

            if (Type == 'I')
            {
                Byte = new byte[Length + 4];
                Byte[0] = byte.Parse("FF", System.Globalization.NumberStyles.AllowHexSpecifier);
                Byte[1] = Convert.ToByte(Type);
                Byte[2] = Convert.ToByte(Length);
                for (int i = 3, j = 0; i < Length + 3; i++, j++)
                {
                    Byte[i] = InfByte[j];
                }
                Byte[Length + 3] = byte.Parse("FF", System.Globalization.NumberStyles.AllowHexSpecifier);
            }
            else
            {
                Byte = new byte[0];
            }
            return Byte;
        }
        byte[] FrameForming(byte[] InfByte, char Type, long Length, long FileLen)
        {
            byte[] Byte;
            if (Type == 'H')
            {
                Byte = new byte[Length + 5];
                Byte[0] = byte.Parse("FF", System.Globalization.NumberStyles.AllowHexSpecifier);
                Byte[1] = Convert.ToByte(Type);
                Byte[2] = Convert.ToByte(Length);
                Byte[3] = Convert.ToByte(FileLen);
                for (int i = 4, j = 0; i < Length + 4; i++, j++)
                {
                    Byte[i] = InfByte[j];
                }
                Byte[Length + 4] = byte.Parse("FF", System.Globalization.NumberStyles.AllowHexSpecifier);
            }
            else
            {
                Byte = new byte[0];
            }
            return Byte;
        }

        public void FrameReceiving()
        {
            while (true)
            {
                while ((Connection == true) & (ComPort.CtsHolding == true))//CTS-модем готов к приему, есть соединение
                {
                    string mess = "";
                    string message = "";
                    string s1 = "";
                    for (int i = 0; ComPort.BytesToRead > 0; i++)//в буф COM есть байты для чтения
                    {
                        s1 = Convert.ToString(ComPort.ReadByte(), 2);//чтение байтов
                        if (s1.Count() < 8)
                        {
                            for (int j = 0; s1.Count() < 8; j++)
                            {
                                s1 = "0" + s1;
                            }
                        }
                        message += s1;
                        Thread.Sleep(30);
                    }
                    if (message.Count() > 0)//байты получены?
                    {
                        if (CheckSyndr(message) == true)//ПРОВЕРКА ошибки
                        {
                            ErrCount++;
                            if (ErrCount >= 3)
                            {
                                SynchWindow.Send(d => label4.Visibility = Visibility.Hidden, null);
                                SynchWindow.Send(d => buttonSend.IsEnabled = true, null);
                                MessageBox.Show("Ошибка передачи");
                                HeadInfSentRecFALSE();
                                WriteFileStream.Close();
                                WriteFileStream.Dispose();
                                ErrCount = 0;
                            }
                            NAK();
                        }
                        else //декодируем кадр
                        {
                            byte[] DecodedMessage = DecodeFromHemToStr(message);
                            if (DecodedMessage[1] == Convert.ToByte('A')) //подтв успешной передачи
                            {
                                ErrCount = 0;//сброс счетчика ошибок
                                ComPort.RtsEnable = false;//rts-запрос на передачу false
                                if ((HeaderSent == true) & (InformSent == true) & (Confirmation == false))//Заголовок и файл отправлены
                                {
                                    HeadInfSentRecFALSE();
                                    SynchWindow.Send(d => progressBar1.Visibility = Visibility.Hidden, null);
                                    SynchWindow.Send(d => buttonSend.IsEnabled = true, null);
                                    MessageBox.Show("Файл успешно доставлен");
                                    ReadFileStream.Close();//Закрытие потока чтения файла
                                    ReadFileStream.Dispose();
                                }
                                if ((HeaderSent == true) & (InformSent == false) & (Confirmation == true))//Доставлен заголовок, но файл - нет
                                {

                                    SynchWindow.Send(d => buttonSend.IsEnabled = true, null);//????????

                                    SynchWindow.Send(d => progressBar1.Value++, null);
                                    Pos = ReadFileStream.Position;
                                    long k = ReadFileStream.Length - Pos;//длина полученного
                                    if (k > 0)
                                    {
                                        byte[] inf;
                                        if (k > 40)
                                        {
                                            inf = new byte[40];
                                            for (int i = 0; i < 40; i++)
                                            {
                                                inf[i] = Convert.ToByte(ReadFileStream.ReadByte());
                                            }
                                        }
                                        else
                                        {
                                            inf = new byte[k];
                                            for (int i = 0; i < k; i++)
                                            {
                                                inf[i] = Convert.ToByte(ReadFileStream.ReadByte());
                                            }
                                        }
                                        INFO(inf);
                                    }
                                    else//послан весь файл
                                    {
                                        InformSent = true;
                                        Confirmation = false;
                                        END();
                                    }
                                }
                                if ((HeaderSent == false) & (Confirmation == true))//заголовок не послан, но подтвержден?
                                {
                                    HeaderSent = true;//загол доставлен
                                }
                            }
                            if (DecodedMessage[1] == Convert.ToByte('N'))//отриц квит
                            {
                                ErrCount++;
                                if (ErrCount >= 3)//закрываем поток
                                {
                                    SynchWindow.Send(d => progressBar1.Visibility = Visibility.Hidden, null);
                                    SynchWindow.Send(d => buttonSend.IsEnabled = true, null);
                                    MessageBox.Show("Ошибка передачи");
                                    HeadInfSentRecFALSE();
                                    ErrCount = 0;
                                    ReadFileStream.Close();
                                    ReadFileStream.Dispose();
                                }
                                else
                                {
                                    if (HeaderSent == true)
                                    {
                                        byte[] inf;
                                        long k = ReadFileStream.Length - Pos;
                                        ReadFileStream.Position = Pos;
                                        if (k > 40)
                                        {
                                            inf = new byte[50];
                                            for (int i = 0; i < 40; i++)
                                            {
                                                inf[i] = Convert.ToByte(ReadFileStream.ReadByte());
                                            }
                                        }
                                        else
                                        {
                                            inf = new byte[k];
                                            for (int i = 0; i < k; i++)
                                            {
                                                inf[i] = Convert.ToByte(ReadFileStream.ReadByte());
                                            }
                                        }
                                        INFO(inf);//формируем инф кадр
                                    }
                                    else
                                    {
                                        string FileName = SourcePath.Substring(SourcePath.LastIndexOf('\\') + 1);
                                        byte[] Header = new byte[FileName.Count()];
                                        var FileLen = ReadFileStream.Length;
                                        for (int i = 0; i < FileName.Count(); i++)
                                        {
                                            Header[i] = Convert.ToByte(FileName[i]);
                                        }
                                        HEADER(Header, FileLen);//формируем заголовок кадра
                                    }
                                }
                            }
                            if (DecodedMessage[1] == Convert.ToByte('T'))//польз согласен и готов принять
                            {
                                SynchWindow.Send(d => buttonSend.IsEnabled = false, null);
                                SynchWindow.Send(d => progressBar1.Visibility = Visibility.Visible, null);
                                byte[] inf;
                                Pos = ReadFileStream.Position;
                                long k = ReadFileStream.Length - Pos;
                                if (k > 40)
                                {
                                    inf = new byte[50];
                                    for (int i = 0; i < 40; i++)
                                    {
                                        inf[i] = Convert.ToByte(ReadFileStream.ReadByte());
                                    }
                                }
                                else
                                {
                                    inf = new byte[k];
                                    for (int i = 0; i < k; i++)
                                    {
                                        inf[i] = Convert.ToByte(ReadFileStream.ReadByte());
                                    }
                                }
                                Confirmation = false;
                                INFO(inf);    //формируем инф кадр                            
                            }
                            if (DecodedMessage[1] == Convert.ToByte('F'))//отклонение приема
                            {
                                MessageBox.Show("Принимающая сторона отказывается принимать файл!");
                                ReadFileStream.Close();
                                ReadFileStream.Dispose();
                                HeadInfSentRecFALSE();
                            }
                            if (DecodedMessage[1] == Convert.ToByte('E'))//конец передачи
                            {
                                InformRec = true;
                                SynchWindow.Send(d => label4.Visibility = Visibility.Hidden, null);
                                SynchWindow.Send(d => buttonSend.IsEnabled = true, null);
                                MessageBox.Show("Файл принят");
                                ACK();
                                HeadInfSentRecFALSE();
                                WriteFileStream.Close();
                                WriteFileStream.Dispose();
                            }
                            if (DecodedMessage[1] == Convert.ToByte('H'))// кадр загол
                            {
                                if (DecodedMessage.Count() == DecodedMessage[2] + 5)//[стартБайт][типКадра][длиназагол][длина файла в байт][ИнфБайты][стопБайт]
                                {
                                    for (int i = 0; i < Convert.ToInt32(DecodedMessage[2]); i++)//преобраз байты загол в char
                                    {
                                        mess = mess + Convert.ToChar(DecodedMessage[4 + i]);
                                    }
                                    string len = Convert.ToString(DecodedMessage[3]);
                                    HeaderRec = true;
                                    ACK();
                                    if (MessageBox.Show("Принять файл " + mess + " размер-" + len + " байт?", "Согласие на передачу", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                                    {
                                        if (SaveFileTo(mess) == true)//выбрано место дл сохранения
                                        {
                                            HeaderRec = true;//загол получен
                                            SynchWindow.Send(d => buttonSend.IsEnabled = false, null);
                                            SynchWindow.Send(d => label4.Visibility = Visibility.Visible, null);
                                            TRUE();
                                        }
                                        else//загол НЕ сохранен
                                        {
                                            FALSE();
                                            HeadInfSentRecFALSE();
                                        }
                                    }
                                    else//не принимать файл
                                    {
                                        FALSE();
                                        HeadInfSentRecFALSE();
                                    }
                                }
                                else//длина полученного и длина заголовка не совпадают
                                {
                                    ErrCount++;
                                    if (ErrCount >= 3)
                                    {
                                        SynchWindow.Send(d => label4.Visibility = Visibility.Hidden, null);
                                        SynchWindow.Send(d => buttonSend.IsEnabled = true, null);
                                        MessageBox.Show("Ошибка передачи");
                                        HeadInfSentRecFALSE();
                                        ErrCount = 0;
                                        WriteFileStream.Close();
                                        WriteFileStream.Dispose();
                                    }
                                    NAK();
                                }
                            }
                            if (DecodedMessage[1] == Convert.ToByte('I'))//инф кадр
                            {
                                if (DecodedMessage.Count() == DecodedMessage[2] + 4)//[стартБайт][типКадра][длинаИнфЧасти][ИнфБайты][стопБайт]
                                {
                                    ACK();
                                    WriteFileStream.Write(DecodedMessage, 3, Convert.ToInt32(DecodedMessage[2]));
                                }
                                else//длина полученного и длина файла не совпадают
                                {
                                    ErrCount++;
                                    if (ErrCount >= 3)
                                    {
                                        SynchWindow.Send(d => label4.Visibility = Visibility.Hidden, null);
                                        SynchWindow.Send(d => buttonSend.IsEnabled = true, null);
                                        MessageBox.Show("Ошибка передачи");
                                        HeadInfSentRecFALSE();
                                        ErrCount = 0;
                                        WriteFileStream.Close();
                                        WriteFileStream.Dispose();
                                    }
                                    NAK();
                                }
                            }
                        }
                    }
                }
            }
        }

        bool? SaveFileTo(string Header)//выбор места сохранения файла
        {
            SaveFileDialog path = new SaveFileDialog();
            path.AddExtension = true;
            path.Title = "Выберите место для сохранения файла";
            path.FileName = Header;
            bool? ChosenPath = path.ShowDialog();
            if (ChosenPath == true)
            {
                PurposePath = path.FileName;
                WriteFileStream = new FileStream(PurposePath, FileMode.Create, FileAccess.Write);
            }
            return ChosenPath;
        }

        byte[] CodeHemming(byte[] msg)
        {
            int len = msg.Count();
            string str = "";
            string s = "";
            for (int i = 0; i < len; i++)//перевод в 2ичную строку
            {
                s = Convert.ToString(msg[i], 2);
                while (s.Count() < 8)
                {
                    s = "0" + s;
                }
                str = str + s;
            }

            if (string.IsNullOrEmpty(str))
                return new byte[1];
            var returnMsg = new List<byte>();
            var strArr = str.ToCharArray();
            var strIntArr = new int[strArr.Length];
            int x = 0;
            for (var i = 0; i < strArr.Length; i++)
            {
                x = Convert.ToInt32(strArr[i]);
                if (x == 48)
                {
                    strIntArr[i] = 0;
                }
                else if (x == 49)
                {
                    strIntArr[i] = 1;
                }
            }
            for (int k = 0; k < strIntArr.Length / 4; k++)
            {
                var BinChar = new Stack<int>();
                for (int y = 0; y < 4; y++)
                    BinChar.Push(strIntArr[k * 4 + y]);

                if (BinChar.Count != 4)
                    for (var i = 4 - BinChar.Count; i > 0; i--)
                        BinChar.Push(0);

                var binChar = BinChar.ToArray<int>();
                BinChar.Clear();
                var hem = new byte[8];
                hem[0] = 0;
                hem[1] = (byte)binChar[0];
                hem[2] = (byte)binChar[1];
                hem[3] = (byte)binChar[2];
                hem[4] = (byte)(hem[1] ^ hem[2] ^ hem[3]);
                hem[5] = (byte)binChar[3];
                hem[6] = (byte)(hem[1] ^ hem[2] ^ hem[5]);
                hem[7] = (byte)(hem[1] ^ hem[3] ^ hem[5]);
                int bytesToSend = 0;
                for (var a = 0; a < 8; a++)            //переводим в 32ричную систему
                    if (hem[a] == 1)
                        bytesToSend += (int)Math.Pow(2, 7 - (a));
                returnMsg.Add(Convert.ToByte(bytesToSend));
            }

            return returnMsg.ToArray<byte>();
        }

        byte[] DecodeFromHemToStr(string msg)
        {
            var binHem = new List<int>();
            string newMessage = null;
            var arr = new int[16];
            var msgbyte = new byte[msg.Length / 16];
            for (var l = 0; l < msg.Length / 16; l++)
            {
                binHem.Clear();
                for (var k = 0; k < 2; k++)
                {
                    var Chars1 = 0;
                    int x = 0;
                    for (var i = 0; i < 8; i++)
                    {
                        x = msg[l * 16 + k * 8 + i];
                        if (x == 48)
                        {
                            arr[k * 8 + i] = 0;
                        }
                        else if (x == 49)
                        {
                            arr[k * 8 + i] = 1;
                            Chars1 += (int)Math.Pow(2, 7 - i);
                        }
                    }

                    var stack = new Stack<int>();
                    while (Chars1 > 0)
                    {
                        stack.Push(Chars1 % 2);
                        Chars1 /= 2;
                    }
                    if (stack.Count != 8)
                        for (var i = 8 - stack.Count; i > 0; i--)
                            stack.Push(0);
                    binHem.AddRange(stack.ToArray<int>());
                }

                var purecode = new int[8];
                purecode[0] = binHem[5];
                purecode[1] = binHem[3];
                purecode[2] = binHem[2];
                purecode[3] = binHem[1];
                purecode[4] = binHem[13];
                purecode[5] = binHem[11];
                purecode[6] = binHem[10];
                purecode[7] = binHem[9];
                var Chars = 0;
                for (var i = 0; i < 8; i++)
                    if (purecode[i] == 1)
                        Chars += (int)Math.Pow(2, 7 - i);
                newMessage += Convert.ToChar(Chars);
                msgbyte[l] = Convert.ToByte(Convert.ToChar(Chars));
            }
            return msgbyte;
        }

        bool CheckSyndr(string msg)
        {
            var binHem = new List<int>();
            for (var l = 0; l < msg.Length / 8; l++)
            {
                binHem.Clear();
                for (var i = 0; i < 8; i++)
                {
                    binHem.Add(msg[l * 8 + i]);
                }
                var syndr = new int[3];
                syndr[0] = binHem[1] ^ binHem[2] ^ binHem[3] ^ binHem[4];
                syndr[1] = binHem[1] ^ binHem[2] ^ binHem[5] ^ binHem[6];
                syndr[2] = binHem[1] ^ binHem[3] ^ binHem[5] ^ binHem[7];
                var isNull = true;
                for (var i = 0; i < 3; i++)
                {
                    if (syndr[i] == 0) continue;
                    isNull = false;
                    break;
                }
                if (!isNull)
                    return true;
            }
            return false;
        }

        private void MenuItemClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MenuItemAbout_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Программа предназначена для передачи файла между двумя ЭВМ, соединенными нуль-модемно, через COM-порт.\nДля передачи файла настройте COM порт и нажмите кнопку \"Отправить файл\" ", "О программе");
        }

        private void MenuItemStud_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Исполнитель:\tСоболева Е.Д.", "Разработано");
        }

    }

}