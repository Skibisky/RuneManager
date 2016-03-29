﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Imaging;

namespace RuneApp
{
    // A really overloaded control to display runes cool-like with a million options
    // Mostly stolen from TransparentControl
    public class RuneControl : Control
    {
        private readonly Timer refresher;

        // yeah, pictures
        private Image _imageSlot;
        private Image _imageSet;
        private Image _imageBack;
        private Image _imageStars;

        // allows runes to look "selected"
        private float gamma;

        // if to render fx
        private bool renderStars;
        private bool renderBack;

        // Number of stars
        private int grade;

        // Normal, Magic, Rare, Hero, Legend
        private int coolness;

        public RuneControl()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            gamma = 1;
            refresher = new Timer();
            refresher.Tick += TimerOnTick;
            refresher.Interval = 50;
            refresher.Enabled = true;
            refresher.Start();
            grade = 1;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x20;
                return cp;
            }
        }

        protected override void OnMove(EventArgs e)
        {
            RecreateHandle();
        }

        public void SetRune(object obj)
        {
            Tag = obj;
            if (obj == null)
                return;

            RuneOptim.Rune rune = (RuneOptim.Rune)obj;
            grade = rune.Grade;
            switch (rune.Slot)
            {
                case 1:
                    _imageSlot = global::RuneApp.Runes.rune1;
                    break;
                case 2:
                    _imageSlot = global::RuneApp.Runes.rune2;
                    break;
                case 3:
                    _imageSlot = global::RuneApp.Runes.rune3;
                    break;
                case 4:
                    _imageSlot = global::RuneApp.Runes.rune4;
                    break;
                case 5:
                    _imageSlot = global::RuneApp.Runes.rune5;
                    break;
                case 6:
                    _imageSlot = global::RuneApp.Runes.rune6;
                    break;

            }
            switch (rune.Set)
            {
                case RuneOptim.RuneSet.Blade:
                    _imageSet = global::RuneApp.Runes.blade;
                    break;
                case RuneOptim.RuneSet.Despair:
                    _imageSet = global::RuneApp.Runes.despair;
                    break;
                case RuneOptim.RuneSet.Destroy:
                    _imageSet = global::RuneApp.Runes.destroy;
                    break;
                case RuneOptim.RuneSet.Endure:
                    _imageSet = global::RuneApp.Runes.endure;
                    break;
                case RuneOptim.RuneSet.Energy:
                    _imageSet = global::RuneApp.Runes.energy;
                    break;
                case RuneOptim.RuneSet.Fatal:
                    _imageSet = global::RuneApp.Runes.fatal;
                    break;
                case RuneOptim.RuneSet.Focus:
                    _imageSet = global::RuneApp.Runes.focus;
                    break;
                case RuneOptim.RuneSet.Guard:
                    _imageSet = global::RuneApp.Runes.guard;
                    break;
                case RuneOptim.RuneSet.Nemesis:
                    _imageSet = global::RuneApp.Runes.nemesis;
                    break;
                case RuneOptim.RuneSet.Rage:
                    _imageSet = global::RuneApp.Runes.rage;
                    break;
                case RuneOptim.RuneSet.Revenge:
                    _imageSet = global::RuneApp.Runes.revenge;
                    break;
                case RuneOptim.RuneSet.Shield:
                    _imageSet = global::RuneApp.Runes.shield;
                    break;
                case RuneOptim.RuneSet.Swift:
                    _imageSet = global::RuneApp.Runes.swift;
                    break;
                case RuneOptim.RuneSet.Vampire:
                    _imageSet = global::RuneApp.Runes.vampire;
                    break;
                case RuneOptim.RuneSet.Violent:
                    _imageSet = global::RuneApp.Runes.violent;
                    break;
                case RuneOptim.RuneSet.Will:
                    _imageSet = global::RuneApp.Runes.will;
                    break;

            }


            _imageBack = global::RuneApp.Runes.bg_normal;
            coolness = 0;
            if (rune.Sub4Type != RuneOptim.Attr.Null)
            {
                _imageBack = global::RuneApp.Runes.bg_legend;
                coolness = 4;
            }
            else if (rune.Sub3Type != RuneOptim.Attr.Null)
            {
                _imageBack = global::RuneApp.Runes.bg_hero;
                coolness = 3;
            }
            else if (rune.Sub2Type != RuneOptim.Attr.Null)
            {
                _imageBack = global::RuneApp.Runes.bg_rare;
                coolness = 2;
            }
            else if (rune.Sub1Type != RuneOptim.Attr.Null)
            {
                _imageBack = global::RuneApp.Runes.bg_magic;
                coolness = 1;
            }

            _imageStars = global::RuneApp.Runes.star_unawakened;
            if (rune.Level == 15)
                _imageStars = global::RuneApp.Runes.star_awakened;

            Refresh();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (_imageSlot != null)
            {
                var attr = new System.Drawing.Imaging.ImageAttributes();
                int top = Top;
                int bottom = Top + Height;
                int left = Left;
                int right = Left + Width;

                attr.SetGamma(gamma);

                int squarish = Math.Max(_imageSlot.Width, _imageSlot.Height);
                squarish = (int)(squarish * 1.25);

                if (renderBack)
                    e.Graphics.DrawImage(_imageBack,
                        new Rectangle((Width / 2) - (squarish / 2), (Height / 2) - (squarish / 2), squarish, squarish),
                        new Rectangle(0, 0, _imageBack.Width, _imageBack.Height), GraphicsUnit.Pixel);
                    //e.Graphics.DrawImage(_imageBack, (Width / 2) - (_imageBack.Width / 2), (Height / 2) - (_imageBack.Height / 2));

                //e.Graphics.DrawImage(_image, (Width / 2) - (_image.Width / 2), (Height / 2) - (_image.Height / 2));

                //Point[] dest = new Point[]{ new Point(top, left), new Point(top, right), new Point(bottom, right)};
                e.Graphics.DrawImage(_imageSlot, 
                    new Rectangle((Width / 2) - (_imageSlot.Width / 2), (Height / 2) - (_imageSlot.Height / 2), _imageSlot.Width, _imageSlot.Height), 
                    0, 0, _imageSlot.Width, _imageSlot.Height, 
                    GraphicsUnit.Pixel, attr);

                int smallish = (int)(squarish * 0.5);

                if (coolness != 0)
                {
                    float[] colour = new float[] { 0, 0.6f, 0, 0, 1 };

                    if (coolness == 2)
                        colour = new float[] { 0.1f, 0.2f, 0.8f, 0, 1 };
                    else if (coolness == 3)
                        colour = new float[] { 0.8f, 0, 0.8f, 0, 1 };
                    else if (coolness == 4)
                        colour = new float[] { 0.5f, 0.1f, 0, 0, 1 };


                    float[][] ptsArray = 
                    { 
                    new float[] {0.7f, 0, 0, 0, 0},
                    new float[] {0, 0.7f, 0, 0, 0},
                    new float[] {0, 0, 0.7f, 0, 0},
                    new float[] {0, 0, 0, 1, 0}, colour
                    };
                    ColorMatrix clrMatrix = new ColorMatrix(ptsArray);
                    attr.SetColorMatrix(clrMatrix,
                    ColorMatrixFlag.Default,
                    ColorAdjustType.Default);
                }

                if (_imageSet != null)
                {
                    e.Graphics.DrawImage(_imageSet,
                        new Rectangle((Width / 2) - (smallish / 2), (Height / 2) - (smallish / 2), smallish, smallish),
                        0, 0, _imageSet.Width, _imageSet.Height,
                        GraphicsUnit.Pixel, attr);
                }

                // for int grade draw star
                if (renderStars)
                {
                    for (int i = 0; i < grade; i++)
                    {
                        e.Graphics.DrawImage(_imageStars,
                            new Rectangle((Width / 2) - (squarish / 2) + 2 + (8 + 6 / grade) * i, (Height / 2) - (squarish / 2) + 3, 13, 13),
                            new Rectangle(0, 0, _imageStars.Width, _imageStars.Height),
                            GraphicsUnit.Pixel);
                    }
                }

            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            //Do not paint background
        }

        //Hack
        public void Redraw()
        {
            RecreateHandle();
        }

        private void TimerOnTick(object source, EventArgs e)
        {
            RecreateHandle();
            refresher.Stop();
        }

        public Image SlotImage
        {
            get
            {
                return _imageSlot;
            }
            set
            {
                _imageSlot = value;
                RecreateHandle();
            }
        }

        public Image SetImage
        {
            get
            {
                return _imageSet;
            }
            set
            {
                _imageSet = value;
                RecreateHandle();
            }
        }

        public Image StarImage
        {
            get
            {
                return _imageStars;
            }
            set
            {
                _imageStars = value;
                RecreateHandle();
            }
        }

        public Image BackImage
        {
            get
            {
                return _imageBack;
            }
            set
            {
                _imageBack = value;
                RecreateHandle();
            }
        }

        public float Gamma
        {
            get
            {
                return gamma;
            }
            set
            {
                gamma = value;
            }
        }

        public int Grade { get { return grade; } set { grade = value; } }
        public bool ShowBack { get { return renderBack; } set { renderBack = value; } }
        public bool ShowStars { get { return renderStars; } set { renderStars = value; } }
        public int Coolness { get { return coolness; } set { coolness = value; } }
    }
}