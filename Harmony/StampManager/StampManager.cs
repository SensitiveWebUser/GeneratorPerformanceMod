using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using WorldGenerationEngineFinal;

namespace MyTestMod.Harmony.StampManager
{
    internal class StampManager
    {

        [HarmonyPatch(typeof(WorldGenerationEngineFinal.StampManager), "DrawStamp")]
        [HarmonyPatch(new Type[] { typeof(Color32[]), typeof(Color[]), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(float), typeof(float), typeof(bool), typeof(bool), typeof(Color), typeof(float), typeof(float), typeof(bool), typeof(bool) })]
        private static class DrawStamp
        {

            private static readonly Color rotatedColorNone = Color.clear;

            [HarmonyPrefix]
            private static bool Prefix(Color32[] _dest, Color[] _src, int _x, int _y, int _destWidth, int _destHeight, int _srcWidth, int _srcHeight, float _alpha = 1f, float _scale = 1f, bool _center = false, bool _colorOverride = false, Color _color = default(Color), float _biomeCutoff = 0.1f, float _angle = 0f, bool isWater = false, bool isRiverStamp = false)
            {
                NewDrawStampMethod(_dest, _src, _x, _y, _destWidth, _destHeight, _srcWidth, _srcHeight, _alpha, _scale, _center, _colorOverride, _color, _biomeCutoff, _angle, isWater, isRiverStamp);
                return false;

            }

            private static void NewDrawStampMethod(Color32[] _dest, Color[] _src, int _x, int _y, int _destWidth, int _destHeight, int _srcWidth, int _srcHeight, float _alpha = 1f, float _scale = 1f, bool _center = false, bool _colorOverride = false, Color _color = default(Color), float _biomeCutoff = 0.1f, float _angle = 0f, bool isWater = false, bool isRiverStamp = false)
            {
                if (_center)
                {
                    _x -= (int)(_srcWidth * _scale) / 2;
                    _y -= (int)(_srcHeight * _scale) / 2;
                }

                double num = Math.PI / 180.0 * (double)_angle;
                double sine = Math.Sin(num);
                double cosine = Math.Cos(num);
                int num2 = Mathf.FloorToInt(((int)Mathf.Sqrt(_srcWidth * _srcWidth + _srcHeight * _srcHeight) - _srcWidth) / 2 * _scale);
                int num3 = Mathf.FloorToInt(_srcWidth * _scale + num2);
                num2 = -num2;
                int num4 = num2;
                int num5 = _x + num2;
                if (num5 < 0)
                {
                    num4 -= num5;
                }

                int num6 = num3;
                num5 = _x + num3;
                if (num5 >= _destWidth)
                {
                    num6 -= num5 - _destWidth;
                }

                int num7 = num2;
                int num8 = _y + num2;
                if (num8 < 0)
                {
                    num7 -= num8;
                }

                int num9 = num3;
                num8 = _y + num3;
                if (num8 >= _destHeight)
                {
                    num9 -= num8 - _destHeight;
                }

                for (int i = num7; i < num9; i++)
                {
                    int num10 = (_y + i) * _destWidth;
                    float y = i / _scale;
                    for (int j = num4; j < num6; j++)
                    {
                        int num11 = _x + j + num10;
                        if (isWater && _dest[num11].b > 0f)
                        {
                            continue;
                        }

                        Color rotatedColor = GetRotatedColor(j / _scale, y, _src, sine, cosine, _srcWidth, _srcHeight, isWater);
                        if (rotatedColor.r + rotatedColor.g + rotatedColor.b + rotatedColor.a < 1E-05f)
                        {
                            continue;
                        }

                        if (isWater)
                        {
                            if (_colorOverride)
                            {
                                _dest[num11] = _color;
                            }
                            else
                            {
                                _dest[num11] = rotatedColor;
                            }

                            continue;
                        }

                        if (_colorOverride)
                        {
                            if (rotatedColor.a > _biomeCutoff)
                            {
                                _dest[num11] = _color;
                            }

                            continue;
                        }

                        float num12 = rotatedColor.a * _alpha;
                        Color color = _dest[num11];
                        color.r += (rotatedColor.r - color.r) * num12;
                        color.g += (rotatedColor.g - color.g) * num12;
                        color.b += (rotatedColor.b - color.b) * num12;
                        _dest[num11] = color;
                    }
                }
            }
            private static Color GetRotatedColor(float x1, float y1, Color[] src, double sine, double cosine, int width, int height, bool isWater = false)
            {
                int halfWidth = width / 2;
                int halfHeight = height / 2;
                float rotatedX = (float)(cosine * (x1 - halfWidth) + sine * (y1 - halfHeight) + halfWidth);
                int floorRotatedX = (int)rotatedX;
                if (floorRotatedX < 0 || floorRotatedX >= width)
                {
                    return rotatedColorNone;
                }

                float rotatedY = (float)((0.0 - sine) * (x1 - halfWidth) + cosine * (y1 - halfHeight) + halfHeight);
                int floorRotatedY = (int)rotatedY;
                if (floorRotatedY < 0 || floorRotatedY >= height)
                {
                    return rotatedColorNone;
                }

                int index = floorRotatedX + floorRotatedY * width;
                Color color = src[index];
                Color colorRight = (floorRotatedX + 1 >= width) ? color : src[index + 1];
                Color colorDown = (floorRotatedY + 1 >= height) ? color : src[index + width];
                Color colorDiagonal = (floorRotatedX + 1 >= width || floorRotatedY + 1 >= height) ? color : src[index + width + 1];
                if (isWater && (color.b > 0f || colorDown.b > 0f || colorRight.b > 0f || colorDiagonal.b > 0f))
                {
                    return color;
                }

                float xDiff = rotatedX - floorRotatedX;
                float yDiff = rotatedY - floorRotatedY;
                color = InterpolateColors(color, colorRight, xDiff);
                Color interpolatedDown = InterpolateColors(colorDown, colorDiagonal, xDiff);
                color = InterpolateColors(color, interpolatedDown, yDiff);
                return color;
            }

            private static Color InterpolateColors(Color color1, Color color2, float factor)
            {
                color1.r += (color2.r - color1.r) * factor;
                color1.g += (color2.g - color1.g) * factor;
                color1.b += (color2.b - color1.b) * factor;
                color1.a += (color2.a - color1.a) * factor;
                return color1;
            }
        }
    }
}
