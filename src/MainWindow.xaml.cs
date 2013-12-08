using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using System.Runtime.InteropServices;
using FreeImageAPI;
using Microsoft.Win32;
using System.Windows.Forms;
using System.Collections;
using System.Drawing;

namespace GrimReaper
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        string filename;
        string matfilename;
        FileStream fileStreamMAT;
        FileStream fileStreamPAL;
        byte[] tagMAT = new byte[4];
        byte[] tagCMP = new byte[4];
        byte[] buffer = new byte[4];
        byte[] bufrev = new byte[3];
        byte[] actpal = new byte[3];
        bool valid = false;
        int lastSelection;
        int numimagesGlobal;

        MemoryStream palette = new MemoryStream();
        MemoryStream complete = new MemoryStream();
        MemoryStream tempACT = new MemoryStream();
        MemoryStream TGA = new MemoryStream();
        MemoryStream PAL = new MemoryStream();
        MemoryStream[] Images;
        bool palloaded = false;
        int w;
        int newmat = 0;

        int[] flipped = new int[64];

        public MainWindow()
        {
            InitializeComponent();
        }

        public static byte[] Read(string fileNames)
        {
            byte[] buff = null;

            FileStream fs = new FileStream(fileNames, FileMode.Open, FileAccess.Read);
            BinaryReader br = new BinaryReader(fs);
            long numBytes = new FileInfo(fileNames).Length;
            buff = br.ReadBytes((int)numBytes);
            return buff;
        }

        public void TGAMaker(MemoryStream img)
        {
            newmat = 1;
            complete.Position = 0;

            img.Position = 12;
            int imagesnum = img.ReadByte();
            numimagesGlobal = imagesnum;
            Images = new MemoryStream[imagesnum];


            img.Position = 0x4c;
            img.Read(buffer, 0, 4);
            int offset = BitConverter.ToInt32(buffer, 0);
            if (offset == 0x8)
                offset = 16;
            else if (offset != 0)
                System.Windows.MessageBox.Show("Unknown offset! " + offset);

            img.Position = (60 + imagesnum * 40 + offset);

            for (int i = 0; i < imagesnum; i++)
            {
                img.Read(buffer, 0, 4);
                byte[] bwidth = new byte[2];
                bwidth[0] = buffer[0];
                bwidth[1] = buffer[1];
                string f = Encoding.UTF8.GetString(bwidth);
                int width = BitConverter.ToInt32(buffer, 0);

                img.Read(buffer, 0, 4);
                byte[] bheight = new byte[2];
                bheight[0] = buffer[0];
                bheight[1] = buffer[1];
                string r = Encoding.UTF8.GetString(bheight);
                int height = BitConverter.ToInt32(buffer, 0);

                img.Read(buffer, 0, 4);
                int hasAlpha = BitConverter.ToInt32(buffer, 0);
                w = width;
                img.Position += 12;
                byte[] pixbuffer = new byte[width * height];
                img.Read(pixbuffer, 0, pixbuffer.Length);
                int pixData = BitConverter.ToInt32(buffer, 0);

                complete.WriteByte(0x0);

                if (palloaded == true)
                    complete.WriteByte(0x1);
                else
                    complete.WriteByte(0x0);

                complete.WriteByte(0x1);
                complete.WriteByte(0x0);
                complete.WriteByte(0x0);
                complete.WriteByte(0x0);
                complete.WriteByte(0x1);
                complete.WriteByte(0x18);
                complete.WriteByte(0x0);
                complete.WriteByte(0x0);
                complete.WriteByte(0x0);
                complete.WriteByte(0x0);
                complete.Write(bwidth, 0, 2);
                complete.Write(bheight, 0, 2);
                complete.WriteByte(0x08);
                complete.WriteByte(0x20);
                palette.Position = 0;

                if (palloaded == true)
                {
                    while (palette.Position != palette.Length)
                    {
                        palette.Read(bufrev, 0, 3);
                        Array.Reverse(bufrev);
                        complete.Write(bufrev, 0, 3);
                        tempACT.Write(bufrev, 0, 3);
                    }
                }

                complete.Write(pixbuffer, 0, pixbuffer.Length);
                complete.Position = 0;
                string str = System.IO.Path.GetFileName(matfilename);
                string[] split = str.Split('.');
                imagesListBox.Items.Add(split[0] + i);
                Images[i] = new MemoryStream();
                complete.CopyTo(Images[i]);
                complete.Flush();
                complete.Position = 0;
            }

            if (numimagesGlobal != null && numimagesGlobal > 1)
            {
                buttonExportAllFrames.IsEnabled = true;
                radioButton1.IsEnabled = true;
                radioButton2.IsEnabled = true;
            }
            else
            {
                buttonExportAllFrames.IsEnabled = false;
                radioButton1.IsEnabled = false;
                radioButton2.IsEnabled = false;
            }
        }

        public void CMPReader(MemoryStream cmp)
        {
            if (cmp.Capacity > 0)
            {
                palloaded = true;
                palette.Position = 0;

                cmp.Position = 0x40;
                byte[] cmpbuffer = new byte[768];
                cmp.Read(cmpbuffer, 0, cmpbuffer.Length);
                palette.Write(cmpbuffer, 0, cmpbuffer.Length);

            }
        }

        private void buttonLoadMAT_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            if (Properties.Settings.Default.matPath != "")
                if (Directory.Exists(Properties.Settings.Default.matPath))
                {
                    dlg.InitialDirectory = Properties.Settings.Default.matPath;
                }

            // Set filter for file extension and default file extension
            dlg.Filter = "Grim Fandango *.MAT|*.MAT";

            // Display OpenFileDialog by calling ShowDialog method
            Nullable<bool> result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox
            if (result == true)
            {
                Properties.Settings.Default.matPath = System.IO.Path.GetDirectoryName(dlg.FileName);
                Properties.Settings.Default.Save();

                // Open document
                matfilename = dlg.FileName;

                //Read MAT into Stream
                fileStreamMAT = File.OpenRead(matfilename);
                TGA = new MemoryStream();
                TGA.SetLength(fileStreamMAT.Length);
                fileStreamMAT.Read(TGA.GetBuffer(), 0, (int)fileStreamMAT.Length);
                TGA.Seek(0, SeekOrigin.Begin);

                byte[] imgnum = new byte[4];

                TGA.Position = 12;
                TGA.Read(imgnum, 0, 4);
                int numint = BitConverter.ToInt16(imgnum, 0);
                textBlock10.Text = numint.ToString();

                TGA.Position = 0;
                TGA.Read(tagMAT, 0, 4);
                string m = Encoding.ASCII.GetString(tagMAT);

                if (m != "MAT ")
                {
                    System.Windows.MessageBox.Show("Invalid File! Grim Fandango *.MAT files only!");
                    valid = false;
                }

                else
                {
                    valid = true;
                    textBlock1.Text = matfilename;

                    FIBITMAP bitmap = FreeImage.Load(FREE_IMAGE_FORMAT.FIF_JPEG, matfilename, FREE_IMAGE_LOAD_FLAGS.JPEG_ACCURATE);
                    MemoryStream memStr = new MemoryStream();
                    lastSelection = imagesListBox.SelectedIndex;
                }
            }
        }

        private void buttonLoadPAL_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            if ( Properties.Settings.Default.palPath != "")
                if (Directory.Exists(Properties.Settings.Default.palPath))
                {
                    dlg.InitialDirectory = Properties.Settings.Default.palPath;
                }


            // Set filter for file extension and default file extension
            dlg.Filter = "Grim Fandango *.cmp|*.cmp";

            // Display OpenFileDialog by calling ShowDialog method
            Nullable<bool> result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox
            if (result == true)
            {
                Properties.Settings.Default.palPath = System.IO.Path.GetDirectoryName(dlg.FileName);
                Properties.Settings.Default.Save();
                // Open document
                filename = dlg.FileName;

                //Read PAL into Stream
                fileStreamPAL = File.OpenRead(filename);
                PAL = new MemoryStream();
                PAL.SetLength(fileStreamPAL.Length);
                fileStreamPAL.Read(PAL.GetBuffer(), 0, (int)fileStreamPAL.Length);
                PAL.Seek(0, SeekOrigin.Begin);


                PAL.Read(tagCMP, 0, 4);
                string m = Encoding.ASCII.GetString(tagCMP);

                if (m != "CMP ")
                    System.Windows.MessageBox.Show("Invalid File! Grim Fandango *.CMP files only!");
                else
                {
                    textBlock2.Text = filename;
                    lastSelection = imagesListBox.SelectedIndex;
                }
            }
        }

        private void buttonCombine_Click(object sender, RoutedEventArgs e)
        {
            if (valid == false)
                System.Windows.MessageBox.Show("Nothing to display!");
            else
            {
                imagesListBox.Items.Clear();
                CMPReader(PAL);
                TGAMaker(TGA);

                for (int i = 0; i < flipped.Length; i++)
                {
                    flipped[i] = 0;
                }
                if (lastSelection < 0)
                    imagesListBox.SelectedIndex = 0;
                else
                    imagesListBox.SelectedIndex = lastSelection;

                if (newmat == 1)
                    if (imagesListBox.SelectedIndex < 0)
                        imagesListBox.SelectedIndex = 0;

                if (imagesListBox.Items.Count > 0)
                {
                    buttonExportMAT.IsEnabled = true;
                    buttonExportMAT.Foreground = new SolidColorBrush(Colors.DarkGreen);
                }
            }
        }

        private void imagesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int s = imagesListBox.SelectedIndex;

            if (s < 0)
                s = 0;

            FIBITMAP tga = FreeImage.LoadFromStream(Images[s], FREE_IMAGE_LOAD_FLAGS.DEFAULT);
            FreeImage.ConvertToType(tga, FREE_IMAGE_TYPE.FIT_BITMAP, false);

            MemoryStream memStr = new MemoryStream();
            FreeImage.SaveToStream(tga, memStr, FREE_IMAGE_FORMAT.FIF_BMP);

            image2.Source = BitmapFrame.Create(memStr, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);

            Images[s].Position = 0;
            MemoryStream desc = new MemoryStream();
            Images[s].CopyTo(desc);
            Images[s].Position = 0;

            byte[] width = new byte[2];
            byte[] height = new byte[2];
            byte[] bits = new byte[1];

            desc.Position = 12;
            desc.Read(width, 0, 2);
            int dwidth = BitConverter.ToInt16(width, 0);
            textBlock7.Text = dwidth.ToString();

            desc.Read(height, 0, 2);
            int dheight = BitConverter.ToInt16(height, 0);
            textBlock8.Text = dheight.ToString();

            desc.Read(bits, 0, 1);
            string dbits = BitConverter.ToString(bits, 0);
            int indbits = Convert.ToInt32(dbits);
            textBlock9.Text = indbits.ToString();
            Images[s].Position = 0;
        }

        private void buttonExportSelFrame_Click(object sender, RoutedEventArgs e)
        {
            if (imagesListBox.SelectedIndex < 0)
                System.Windows.MessageBox.Show("Nothing Selected!");
            else
            {
                Microsoft.Win32.SaveFileDialog saveFileDialog1 = new Microsoft.Win32.SaveFileDialog();

                if (Properties.Settings.Default.exportFramePath != "")
                    if (Directory.Exists(Properties.Settings.Default.exportFramePath))
                    {
                        saveFileDialog1.InitialDirectory = Properties.Settings.Default.exportFramePath;
                    }


                saveFileDialog1.Filter = "Bitmap Image|*.bmp|Targa Image|*.tga";
                saveFileDialog1.Title = "Saving the Frame";

                saveFileDialog1.FileName = imagesListBox.Items[imagesListBox.SelectedIndex].ToString();
                bool? result = saveFileDialog1.ShowDialog();
                if (result ?? true)
                {
                    FIBITMAP tga = FreeImage.LoadFromStream(Images[imagesListBox.SelectedIndex], FREE_IMAGE_LOAD_FLAGS.TARGA_LOAD_RGB888);
                    MemoryStream memStr = new MemoryStream();

                    // If the file name is not an empty string open it for saving.
                    if (saveFileDialog1.FileName != "")
                    {
                        Properties.Settings.Default.exportFramePath = System.IO.Path.GetDirectoryName(saveFileDialog1.FileName);
                        Properties.Settings.Default.Save();
                        // Saves the Image via a FileStream created by the OpenFile method.
                        System.IO.FileStream fs = (System.IO.FileStream)saveFileDialog1.OpenFile();
                        string path = fs.ToString();
                        // Saves the Image in the appropriate ImageFormat based upon the
                        // File type selected in the dialog box.
                        // NOTE that the FilterIndex property is one-based.
                        switch (saveFileDialog1.FilterIndex)
                        {
                            case 1:
                                {
                                    FreeImage.ConvertToType(tga, FREE_IMAGE_TYPE.FIT_BITMAP, false);
                                    FreeImage.SaveToStream(tga, memStr, FREE_IMAGE_FORMAT.FIF_BMP);
                                    byte[] data = memStr.ToArray();
                                    fs.Write(data, 0, data.Length);
                                } break;

                            case 2:
                                {
                                    FreeImage.SaveToStream(tga, memStr, FREE_IMAGE_FORMAT.FIF_TARGA);
                                    byte[] data = memStr.ToArray();
                                    fs.Write(data, 0, data.Length);
                                } break;
                        }

                        fs.Close();
                    }
                }
                else
                    result = false;

            }
        }

        private void buttonExportAllFrames_Click(object sender, RoutedEventArgs e)
        {
            if (radioButton1.IsChecked == false && radioButton2.IsChecked == false)
                System.Windows.MessageBox.Show("Select a Format!");
            else
            {
                FolderBrowserDialog brf = new FolderBrowserDialog();
                brf.ShowDialog();

                if (brf.SelectedPath != "")
                {
                    for (int i = 0; i < numimagesGlobal; i++)
                    {
                        FIBITMAP tga = FreeImage.LoadFromStream(Images[i], FREE_IMAGE_LOAD_FLAGS.TARGA_LOAD_RGB888);
                        MemoryStream memStr = new MemoryStream();

                        if (radioButton1.IsChecked == true)
                        {
                            FreeImage.ConvertToType(tga, FREE_IMAGE_TYPE.FIT_BITMAP, false);
                            FreeImage.SaveToStream(tga, memStr, FREE_IMAGE_FORMAT.FIF_BMP);
                            byte[] data = memStr.ToArray();

                            Stream file = File.OpenWrite(brf.SelectedPath + "\\" + imagesListBox.Items[i].ToString() + ".bmp");
                            file.Write(data, 0, data.Length);
                            memStr.Close();
                            file.Close();
                        }

                        else
                        {
                            FreeImage.SaveToStream(tga, memStr, FREE_IMAGE_FORMAT.FIF_TARGA);
                            byte[] data = memStr.ToArray();

                            Stream file = File.OpenWrite(brf.SelectedPath + "\\" + imagesListBox.Items[i].ToString() + ".tga");
                            file.Write(data, 0, data.Length);
                            memStr.Close();
                            file.Close();
                        }
                    }
                    System.Windows.MessageBox.Show("Operation Complete! Images were dumped in: " + brf.SelectedPath + "\\");
                }
            }

        }

        private void buttonSavePAL_Click(object sender, RoutedEventArgs e)
        {
            CMPReader(PAL);

            if (palloaded == true)
            {
                MemoryStream str = new MemoryStream();

                palette.Position = 0;
                while (palette.Position != palette.Length)
                {
                    palette.Read(actpal, 0, 3);
                    tempACT.Write(actpal, 0, 3);
                }
                palette.Position = 0;

                Microsoft.Win32.SaveFileDialog save = new Microsoft.Win32.SaveFileDialog();

                if (Properties.Settings.Default.actPath != "")
                    if (Directory.Exists(Properties.Settings.Default.actPath))
                    {
                        save.InitialDirectory = Properties.Settings.Default.actPath;
                    }

                save.Filter = "ACT Palette|*.act";
                save.Title = "Saving the Palette";
                save.ShowDialog();

                if (save.FileName != "")
                {
                    Properties.Settings.Default.actPath = System.IO.Path.GetDirectoryName(save.FileName);
                    Properties.Settings.Default.Save();
                    System.IO.FileStream fs = (System.IO.FileStream)save.OpenFile();

                    byte[] data = tempACT.ToArray();
                    fs.Write(data, 0, data.Length);
                    tempACT.Close();
                    fs.Close();
                }
            }
            else
                System.Windows.MessageBox.Show("Nothing is Loaded!");

        }

        private void buttonImportImage_Click(object sender, RoutedEventArgs e)
        {
            if (imagesListBox.SelectedIndex >= 0)
            {

                Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

                if (Properties.Settings.Default.importFramePath != "")
                    if (Directory.Exists(Properties.Settings.Default.importFramePath))
                    {
                        dlg.InitialDirectory = Properties.Settings.Default.importFramePath;
                    }

                dlg.Filter = "Bitmap Image|*.bmp|Targa Image|*.tga";
                dlg.Title = "Importing Image into Frame";

                // Display OpenFileDialog by calling ShowDialog method
                Nullable<bool> result = dlg.ShowDialog();
                MemoryStream memStr = new MemoryStream();

                // Get the selected file name and display in a TextBox
                if (result == true)
                {
                    Properties.Settings.Default.importFramePath = System.IO.Path.GetDirectoryName(dlg.FileName);
                    Properties.Settings.Default.Save();
                    flipped[imagesListBox.SelectedIndex] = 1;
                    // Open document

                    switch (dlg.FilterIndex)
                    {
                        case 1:
                            {

                                string impfile = dlg.FileName;
                                FIBITMAP tga = FreeImage.Load(FREE_IMAGE_FORMAT.FIF_BMP, impfile, FREE_IMAGE_LOAD_FLAGS.DEFAULT);
                                Images[imagesListBox.SelectedIndex].Flush();
                                Images[imagesListBox.SelectedIndex].Position = 0;
                                FreeImage.SaveToStream(tga, Images[imagesListBox.SelectedIndex], FREE_IMAGE_FORMAT.FIF_TARGA);

                                Images[imagesListBox.SelectedIndex].Position = 0;
                                MemoryStream desc = new MemoryStream();
                                Images[imagesListBox.SelectedIndex].CopyTo(desc);
                                Images[imagesListBox.SelectedIndex].Position = 0;

                                byte[] width = new byte[2];
                                byte[] height = new byte[2];
                                byte[] bits = new byte[1];

                                desc.Position = 12;
                                desc.Read(width, 0, 2);
                                int dwidth = BitConverter.ToInt16(width, 0);
                                textBlock7.Text = dwidth.ToString();

                                desc.Read(height, 0, 2);
                                int dheight = BitConverter.ToInt16(height, 0);
                                textBlock8.Text = dheight.ToString();

                                desc.Read(bits, 0, 1);
                                string dbits = BitConverter.ToString(bits, 0);
                                int indbits = Convert.ToInt32(dbits);
                                textBlock9.Text = indbits.ToString();
                                desc.Close();
                                Images[imagesListBox.SelectedIndex].Position = 0;

                                FreeImage.ConvertToType(tga, FREE_IMAGE_TYPE.FIT_BITMAP, false);

                                MemoryStream memStr2 = new MemoryStream();
                                FreeImage.SaveToStream(tga, memStr2, FREE_IMAGE_FORMAT.FIF_BMP);

                                image2.Source = BitmapFrame.Create(memStr2, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);

                                memStr2.Close();

                            } break;

                        case 2:
                            {
                                string impfile = dlg.FileName;
                                FIBITMAP tga = FreeImage.Load(FREE_IMAGE_FORMAT.FIF_TARGA, impfile, FREE_IMAGE_LOAD_FLAGS.DEFAULT);
                                Images[imagesListBox.SelectedIndex].Flush();
                                Images[imagesListBox.SelectedIndex].Position = 0;
                                FreeImage.SaveToStream(tga, Images[imagesListBox.SelectedIndex], FREE_IMAGE_FORMAT.FIF_TARGA);

                                Images[imagesListBox.SelectedIndex].Position = 0;
                                MemoryStream desc = new MemoryStream();
                                Images[imagesListBox.SelectedIndex].CopyTo(desc);
                                Images[imagesListBox.SelectedIndex].Position = 0;

                                byte[] width = new byte[2];
                                byte[] height = new byte[2];
                                byte[] bits = new byte[1];

                                desc.Position = 12;
                                desc.Read(width, 0, 2);
                                int dwidth = BitConverter.ToInt16(width, 0);
                                textBlock7.Text = dwidth.ToString();

                                desc.Read(height, 0, 2);
                                int dheight = BitConverter.ToInt16(height, 0);
                                textBlock8.Text = dheight.ToString();

                                desc.Read(bits, 0, 1);
                                string dbits = BitConverter.ToString(bits, 0);
                                int indbits = Convert.ToInt32(dbits);
                                textBlock9.Text = indbits.ToString();
                                desc.Close();
                                Images[imagesListBox.SelectedIndex].Position = 0;


                                FreeImage.ConvertToType(tga, FREE_IMAGE_TYPE.FIT_BITMAP, false);

                                MemoryStream memStr2 = new MemoryStream();
                                FreeImage.SaveToStream(tga, memStr2, FREE_IMAGE_FORMAT.FIF_BMP);


                                image2.Source = BitmapFrame.Create(memStr2, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);

                                memStr2.Close();
                            } break;
                    }
                }
            }
            else
                System.Windows.MessageBox.Show("No frames are selected!");
        }

        private void MATSaver(MemoryStream[] images)
        {

            FileStream fileStream = File.OpenRead(matfilename);
            MemoryStream NEWMAT = new MemoryStream();
            NEWMAT.SetLength(fileStream.Length);
            fileStream.Read(NEWMAT.GetBuffer(), 0, (int)fileStream.Length);

            byte[] buffer = new byte[4];
            byte[] buffer2 = new byte[28];
            byte[] buffer3 = new byte[40];
            byte[] buffer4 = new byte[2];
            byte[] buffer5 = new byte[16];

            MemoryStream MAT = new MemoryStream();
            MAT.WriteByte(0x4D);
            MAT.WriteByte(0x41);
            MAT.WriteByte(0x54);
            MAT.WriteByte(0x20);
            MAT.WriteByte(0x32);
            MAT.WriteByte(0x00);
            MAT.WriteByte(0x00);
            MAT.WriteByte(0x00);
            MAT.WriteByte(0x02); //RLE NOT IMPLEMENTED  
            MAT.WriteByte(0x00);
            MAT.WriteByte(0x00);
            MAT.WriteByte(0x00); //0x0C Begins

            MAT.WriteByte(Convert.ToByte(numimagesGlobal));
            MAT.WriteByte(0x00);
            MAT.WriteByte(0x00);
            MAT.WriteByte(0x00); //0x10 Begins

            MAT.WriteByte(Convert.ToByte(numimagesGlobal));
            MAT.WriteByte(0x00);
            MAT.WriteByte(0x00);
            MAT.WriteByte(0x00); //0x14 Begins

            MAT.WriteByte(0x00);
            MAT.WriteByte(0x00);
            MAT.WriteByte(0x00);
            MAT.WriteByte(0x00); //0x18 Begins

            MAT.WriteByte(0x08); //Number of bits
            MAT.WriteByte(0x00);
            MAT.WriteByte(0x00);
            MAT.WriteByte(0x00); //0x1C Begins

            NEWMAT.Position = 28;
            NEWMAT.Read(buffer, 0, 4);
            MAT.Write(buffer, 0, 4); //0x20 Begins

            MAT.WriteByte(0x40);
            MAT.WriteByte(0x00);
            MAT.WriteByte(0x00);
            MAT.WriteByte(0x00); //0x24 Begins

            NEWMAT.Position = 36;
            NEWMAT.Read(buffer, 0, 4);
            MAT.Write(buffer, 0, 4); //0x28 Begins

            MAT.WriteByte(0x80); //Number of bits
            MAT.WriteByte(0x0A);
            MAT.WriteByte(0x00);
            MAT.WriteByte(0x00); //0x2C Begins

            NEWMAT.Position = 44;
            NEWMAT.Read(buffer2, 0, 28);
            MAT.Write(buffer2, 0, 28); //0x4C Begins

            if (numimagesGlobal == 1)
            {
                NEWMAT.Position = 78;
                NEWMAT.Read(buffer, 0, 4);
                MAT.Write(buffer, 0, 4); //0x4C Begins

                NEWMAT.Position = 76;
                NEWMAT.Read(buffer3, 0, 40);
                MAT.Write(buffer3, 0, 40); //0x74 Begins

                images[0].Position = 12;

                Images[0].Read(buffer4, 0, 2);
                int width = BitConverter.ToInt16(buffer4, 0);
                MAT.Write(buffer4, 0, 2);
                MAT.WriteByte(0x00);
                MAT.WriteByte(0x00);

                Images[0].Read(buffer4, 0, 2);
                int height = BitConverter.ToInt16(buffer4, 0);
                MAT.Write(buffer4, 0, 2);
                MAT.WriteByte(0x00);
                MAT.WriteByte(0x00);

                NEWMAT.Position = 124;
                NEWMAT.Read(buffer5, 0, 16);
                MAT.Write(buffer5, 0, 16); //ImageData begins

                images[0].Position = 786;
                byte[] image = new byte[width * height];
                Images[0].Read(image, 0, width * height);

                if (flipped[0] == 1)
                {
                    BitArray bits = new BitArray(image);
                    BitArray flippedBits = new BitArray(bits);

                    for (int i = 0, j = bits.Length - 1; i < bits.Length; i++, j--)
                    {
                        flippedBits[i] = bits[j];
                    }

                    image = toBitArray.ToByteArray(flippedBits);

                    byte[] flipw = new byte[width];
                    byte[] image2 = new byte[width * height];
                    MemoryStream temp = new MemoryStream();
                    temp.Write(image, 0, image.Length);
                    MemoryStream temp1 = new MemoryStream();

                    int rows = 0;
                    temp.Position = 0;
                    for (rows = 0; rows < height; rows++)
                    {
                        temp.Read(flipw, 0, width);
                        Array.Reverse(flipw);
                        temp1.Write(flipw, 0, width);
                        temp.Position = rows * width;
                    }
                    temp1.Position = 0;
                    image2 = temp1.ToArray();

                    MAT.Write(image2, 0, image2.Length); //Writing Complete
                }
                else
                    MAT.Write(image, 0, image.Length);


                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();

                if (Properties.Settings.Default.exportMATPath != "")
                    if (Directory.Exists(Properties.Settings.Default.exportMATPath))
                    {
                        dlg.InitialDirectory = Properties.Settings.Default.exportMATPath;
                    }

                dlg.Filter = "Grim Fandango *.MAT file|*.mat";
                dlg.Title = "Saving the MAT";
                dlg.ShowDialog();

                if (dlg.FileName != "")
                {
                    Properties.Settings.Default.exportMATPath = System.IO.Path.GetDirectoryName(dlg.FileName);
                    Properties.Settings.Default.Save();

                    System.IO.FileStream fs = (System.IO.FileStream)dlg.OpenFile();

                    byte[] data = MAT.ToArray();
                    fs.Write(data, 0, data.Length);
                    MAT.Close();
                    fs.Close();
                }

            }

            else if (numimagesGlobal > 1)
            {
                NEWMAT.Position = 72;
                byte[] foreheader = new byte[numimagesGlobal * 40];
                NEWMAT.Read(foreheader, 0, foreheader.Length);
                MAT.Write(foreheader, 0, foreheader.Length); //0x1D8 Begins

                NEWMAT.Read(buffer, 0, 4);
                MAT.Write(buffer, 0, 4); //0x1DC Begins

                for (int i = 0; i < numimagesGlobal; i++)
                {
                    images[i].Position = 12;
                    Images[i].Read(buffer4, 0, 2);
                    int width = BitConverter.ToInt16(buffer4, 0);
                    MAT.Write(buffer4, 0, 2);
                    MAT.WriteByte(0x00);
                    MAT.WriteByte(0x00);

                    Images[i].Read(buffer4, 0, 2);
                    int height = BitConverter.ToInt16(buffer4, 0);
                    MAT.Write(buffer4, 0, 2);
                    MAT.WriteByte(0x00);
                    MAT.WriteByte(0x00);

                    NEWMAT.Read(buffer5, 0, 16);
                    MAT.Write(buffer5, 0, 16); //0x1DC Begins


                    images[i].Position = 786;
                    byte[] image = new byte[width * height];
                    Images[i].Read(image, 0, width * height);

                    if (flipped[i] == 1)
                    {
                        BitArray bits = new BitArray(image);
                        BitArray flippedBits = new BitArray(bits);

                        for (int k = 0, j = bits.Length - 1; k < bits.Length; k++, j--)
                        {
                            flippedBits[k] = bits[j];
                        }

                        image = toBitArray.ToByteArray(flippedBits);

                        byte[] flipw = new byte[width];
                        byte[] image2 = new byte[width * height];
                        MemoryStream temp = new MemoryStream();
                        temp.Write(image, 0, image.Length);
                        MemoryStream temp1 = new MemoryStream();

                        int rows = 0;
                        temp.Position = 0;
                        for (rows = 0; rows < height; rows++)
                        {
                            temp.Read(flipw, 0, width);
                            Array.Reverse(flipw);
                            temp1.Write(flipw, 0, width);
                            temp.Position = rows * width;
                        }
                        temp1.Position = 0;
                        image2 = temp1.ToArray();

                        MAT.Write(image2, 0, image2.Length); //Writing Complete
                    }
                    else
                        MAT.Write(image, 0, image.Length);
                }

                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();

                if (Properties.Settings.Default.exportMATPath != "")
                    if (Directory.Exists(Properties.Settings.Default.exportMATPath))
                    {
                        dlg.InitialDirectory = Properties.Settings.Default.exportMATPath;
                    }

                dlg.Filter = "Grim Fandango *.MAT file|*.mat";
                dlg.Title = "Saving the MAT";
                dlg.ShowDialog();

                if (dlg.FileName != "")
                {
                    Properties.Settings.Default.exportMATPath = System.IO.Path.GetDirectoryName(dlg.FileName);
                    Properties.Settings.Default.Save();

                    System.IO.FileStream fs = (System.IO.FileStream)dlg.OpenFile();

                    byte[] data = MAT.ToArray();
                    fs.Write(data, 0, data.Length);
                    MAT.Close();
                    fs.Close();
                }

            }

        }

        private void buttonExportMAT_Click(object sender, RoutedEventArgs e)
        {
            MATSaver(Images);
        }

        private void buttonAbout_Click(object sender, RoutedEventArgs e)
        {
            Credits Wind = new Credits();
            Wind.Owner = Window.GetWindow(this);
            Wind.Show();
        }
    }
}

