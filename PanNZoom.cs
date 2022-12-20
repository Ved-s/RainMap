using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using RainMap;
using System;
using System.Diagnostics;

namespace RainMap
{
    public static class PanNZoom
    {
        
        public static Vector2 Position = Vector2.Zero;

        public static bool Dragging { get; private set; }

        public static Matrix WorldToScreenTransform => Matrix.Multiply(Matrix.CreateTranslation(-Position.X, -Position.Y, 0), Matrix.CreateScale(Zoom));
        public static Matrix ScreenToWorldTransform => Matrix.Multiply(Matrix.CreateScale(1/Zoom), Matrix.CreateTranslation(Position.X, Position.Y, 0));

        public static float Zoom 
        {
            get => zoom;
            set
            {
                zoom = value;
                int zoomFac = 1;
                float zfCheck = zoom;
                while (zfCheck < 1)
                {
                    zfCheck += zfCheck;
                    zoomFac++;
                }
                ZoomFactor = zoomFac;
            }
        }

        public static int ZoomFactor { get; private set; } = 1;

        private static Point DragPos;
        private static int WheelValue;
        private static float WheelZoom;
        private static float zoom = 1f;

        public static void Update()
        {
            MouseState state = Mouse.GetState();

            Point screenPos = state.Position;

            bool drag = state.RightButton == ButtonState.Pressed;

            UpdateDragging(drag, screenPos);

            float wheel = (state.ScrollWheelValue - WheelValue) / 120;
            WheelValue = state.ScrollWheelValue;

            if (wheel == 0)
                return;

            WheelZoom += Math.Sign(wheel) * (float)Math.Pow(2, Math.Abs(wheel));

            float zoom = WheelZoom < 0 ? -1 / (0.2f * WheelZoom - 1) : 0.2f * WheelZoom + 1;
            PanNZoom.SetZoom(zoom, screenPos);
        }

        static void SetZoom(float zoom, Point at) 
        {
            if (zoom == Zoom) return;
            Vector2 atWorldBefore = ScreenToWorld(at.ToVector2());
            Zoom = zoom;
            Vector2 atWorldAfter = ScreenToWorld(at.ToVector2());
            Position -= atWorldAfter - atWorldBefore;
        }

        public static Vector2 WorldToScreen(Vector2 v) 
        {
            v.X = (v.X - Position.X) * Zoom;
            v.Y = (v.Y - Position.Y) * Zoom;
            return v;
        }
        public static Vector2 ScreenToWorld(Vector2 v)
        {
            v.X = (v.X / Zoom) + Position.X;
            v.Y = (v.Y / Zoom) + Position.Y;
            return v;
        }

        static void UpdateDragging(bool drag, Point screenPoint) 
        {
            if (drag && !Dragging)
            {
                DragPos = screenPoint;
                Dragging = true;
            }
            else if (drag && Dragging)
            {
                Position -= screenPoint.Subtract(DragPos).ToVector2() / Zoom;
                DragPos = screenPoint;
            }
            else if (!drag && Dragging) 
            {
                Dragging = false;
            }
        }
    }

    public static class PointExtension
    {
        public static Point Multiply(this Point p, float f)
        {
            return new Point((int)(p.X * f), (int)(p.Y * f));
        }
        public static Point Multiply(this Point p, Point m)
        {
            return new Point((int)(p.X * m.X), (int)(p.Y * m.Y));
        }
        public static Point Divide(this Point p, float f)
        {
            return new Point((int)(p.X / f), (int)(p.Y / f));
        }
        public static Point Subtract(this Point p, Point v)
        {
            return new Point((int)(p.X - v.X), (int)(p.Y - v.Y));
        }
        public static Point Add(this Point p, Point v)
        {
            return new Point((int)(p.X + v.X), (int)(p.Y + v.Y));
        }
        public static Point Add(this Point p, int x, int y)
        {
            return new Point((int)(p.X + x), (int)(p.Y + y));
        }
        public static Vector2 ToVector2(this Point p)
        {
            return new Vector2(p.X, p.Y);
        }
        public static Point ToPoint(this Vector2 v)
        {
            return new Point((int)v.X, (int)v.Y);
        }
    }
}
