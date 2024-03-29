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

                double angleInRadians = Math.PI / 180.0 * _angle;
                double sine = Math.Sin(angleInRadians);
                double cosine = Math.Cos(angleInRadians);
                int offset = Mathf.FloorToInt(((int)Mathf.Sqrt(_srcWidth * _srcWidth + _srcHeight * _srcHeight) - _srcWidth) / 2 * _scale);
                int scaledSrcWidth = Mathf.FloorToInt(_srcWidth * _scale + offset);
                offset = -offset;

                int startX = offset;
                int xPosition = _x + offset;
                if (xPosition < 0)
                {
                    startX -= xPosition;
                }

                int endX = scaledSrcWidth;
                xPosition = _x + scaledSrcWidth;
                if (xPosition >= _destWidth)
                {
                    endX -= xPosition - _destWidth;
                }

                int startY = offset;
                int yPosition = _y + offset;
                if (yPosition < 0)
                {
                    startY -= yPosition;
                }

                int endY = scaledSrcWidth;
                yPosition = _y + scaledSrcWidth;
                if (yPosition >= _destHeight)
                {
                    endY -= yPosition - _destHeight;
                }

                for (int i = startY; i < endY; i++)
                {
                    int destIndex = (_y + i) * _destWidth;
                    float y = i / _scale;
                    for (int j = startX; j < endX; j++)
                    {
                        int index = _x + j + destIndex;
                        if (isWater && _dest[index].b > 0f)
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
                                _dest[index] = _color;
                            }
                            else
                            {
                                _dest[index] = rotatedColor;
                            }

                            continue;
                        }

                        if (_colorOverride)
                        {
                            if (rotatedColor.a > _biomeCutoff)
                            {
                                _dest[index] = _color;
                            }

                            continue;
                        }

                        float alphaFactor = rotatedColor.a * _alpha;
                        Color color = _dest[index];
                        color.r += (rotatedColor.r - color.r) * alphaFactor;
                        color.g += (rotatedColor.g - color.g) * alphaFactor;
                        color.b += (rotatedColor.b - color.b) * alphaFactor;
                        _dest[index] = color;
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
