using System;

namespace Xamarin.Graphics
{
    public class EWAffineTransform
    {
        /**
     * The min value equivalent to zero. If absolute value less then ZERO it considered as zero.  
     */
        private const float Zero = 1E-10f;

        /**
     * The values of transformation matrix
     */
        private float _m00;
        private float _m01;
        private float _m02;
        private float _m10;
        private float _m11;
        private float _m12;

        public EWAffineTransform()
        {
            _m00 = _m11 = 1.0f;
            _m10 = _m01 = _m02 = _m12 = 0.0f;
        }

        public EWAffineTransform(EWAffineTransform t)
        {
            _m00 = t._m00;
            _m10 = t._m10;
            _m01 = t._m01;
            _m11 = t._m11;
            _m02 = t._m02;
            _m12 = t._m12;
        }

        public EWAffineTransform(float m00, float m10, float m01, float m11, float m02, float m12)
        {
            _m00 = m00;
            _m10 = m10;
            _m01 = m01;
            _m11 = m11;
            _m02 = m02;
            _m12 = m12;
        }

        public EWAffineTransform(float[] matrix)
        {
            _m00 = matrix[0];
            _m10 = matrix[1];
            _m01 = matrix[2];
            _m11 = matrix[3];
            if (matrix.Length > 4)
            {
                _m02 = matrix[4];
                _m12 = matrix[5];
            }
        }

        public void SetMatrix(float m00, float m10, float m01, float m11, float m02, float m12)
        {
            _m00 = m00;
            _m10 = m10;
            _m01 = m01;
            _m11 = m11;
            _m02 = m02;
            _m12 = m12;
        }

        public float GetScaleX()
        {
            return _m00;
        }

        public float GetScaleY()
        {
            return _m11;
        }

        public float GetShearX()
        {
            return _m01;
        }

        public float GetShearY()
        {
            return _m10;
        }

        public float GetTranslateX()
        {
            return _m02;
        }

        public float GetTranslateY()
        {
            return _m12;
        }

        public void GetMatrix(float[] matrix)
        {
            matrix[0] = _m00;
            matrix[1] = _m10;
            matrix[2] = _m01;
            matrix[3] = _m11;
            if (matrix.Length > 4)
            {
                matrix[4] = _m02;
                matrix[5] = _m12;
            }
        }

        public float GetDeterminant()
        {
            return _m00 * _m11 - _m01 * _m10;
        }

        public void SetTransform(float m00, float m10, float m01, float m11, float m02, float m12)
        {
            _m00 = m00;
            _m10 = m10;
            _m01 = m01;
            _m11 = m11;
            _m02 = m02;
            _m12 = m12;
        }

        public void SetTransform(EWAffineTransform t)
        {
            SetTransform(t._m00, t._m10, t._m01, t._m11, t._m02, t._m12);
        }

        public void SetToIdentity()
        {
            _m00 = _m11 = 1.0f;
            _m10 = _m01 = _m02 = _m12 = 0.0f;
        }

        public void SetToTranslation(float mx, float my)
        {
            _m00 = _m11 = 1.0f;
            _m01 = _m10 = 0.0f;
            _m02 = mx;
            _m12 = my;
        }

        public void SetToScale(float scx, float scy)
        {
            _m00 = scx;
            _m11 = scy;
            _m10 = _m01 = _m02 = _m12 = 0.0f;
        }

        public void SetToShear(float shx, float shy)
        {
            _m00 = _m11 = 1.0f;
            _m02 = _m12 = 0.0f;
            _m01 = shx;
            _m10 = shy;
        }

        public void SetToRotation(float angle)
        {
            float sin = (float) Math.Sin(angle);
            float cos = (float) Math.Cos(angle);
            if (Math.Abs(cos) < Zero)
            {
                cos = 0.0f;
                sin = sin > 0.0f ? 1.0f : -1.0f;
            }
            else if (Math.Abs(sin) < Zero)
            {
                sin = 0.0f;
                cos = cos > 0.0f ? 1.0f : -1.0f;
            }

            _m00 = _m11 = cos;
            _m01 = -sin;
            _m10 = sin;
            _m02 = _m12 = 0.0f;
        }

        public void SetToRotation(float angle, float px, float py)
        {
            SetToRotation(angle);
            _m02 = px * (1.0f - _m00) + py * _m10;
            _m12 = py * (1.0f - _m00) - px * _m10;
        }

        public static EWAffineTransform GetTranslateInstance(float mx, float my)
        {
            var t = new EWAffineTransform();
            t.SetToTranslation(mx, my);
            return t;
        }

        public static EWAffineTransform GetScaleInstance(float scx, float scY)
        {
            var t = new EWAffineTransform();
            t.SetToScale(scx, scY);
            return t;
        }

        public static EWAffineTransform GetShearInstance(float shx, float shy)
        {
            var m = new EWAffineTransform();
            m.SetToShear(shx, shy);
            return m;
        }

        public static EWAffineTransform GetRotateInstance(float angle)
        {
            var t = new EWAffineTransform();
            t.SetToRotation(angle);
            return t;
        }

        public static EWAffineTransform GetRotateInstance(float angle, float x, float y)
        {
            var t = new EWAffineTransform();
            t.SetToRotation(angle, x, y);
            return t;
        }

        public void Translate(float mx, float my)
        {
            Concatenate(GetTranslateInstance(mx, my));
        }

        public void Scale(float scx, float scy)
        {
            Concatenate(GetScaleInstance(scx, scy));
        }

        public void Shear(float shx, float shy)
        {
            Concatenate(GetShearInstance(shx, shy));
        }

        public void RotateInDegrees(float angle)
        {
            Rotate(Geometry.DegreesToRadians(angle));
        }

        public void RotateInDegrees(float angle, float px, float py)
        {
            Rotate(Geometry.DegreesToRadians(angle), px, py);
        }

        public void Rotate(float angle)
        {
            Concatenate(GetRotateInstance(angle));
        }

        public void Rotate(float angle, float px, float py)
        {
            Concatenate(GetRotateInstance(angle, px, py));
        }

        /** 
     * Multiply matrix of two AffineTransform objects 
     * @param t1 - the AffineTransform object is a multiplicand
     * @param t2 - the AffineTransform object is a multiplier
     * @return an AffineTransform object that is a result of t1 multiplied by matrix t2. 
     */

        private EWAffineTransform Multiply(EWAffineTransform t1, EWAffineTransform t2)
        {
            return new EWAffineTransform(
                t1._m00 * t2._m00 + t1._m10 * t2._m01, // m00
                t1._m00 * t2._m10 + t1._m10 * t2._m11, // m01
                t1._m01 * t2._m00 + t1._m11 * t2._m01, // m10
                t1._m01 * t2._m10 + t1._m11 * t2._m11, // m11
                t1._m02 * t2._m00 + t1._m12 * t2._m01 + t2._m02, // m02
                t1._m02 * t2._m10 + t1._m12 * t2._m11 + t2._m12); // m12
        }

        public void Concatenate(EWAffineTransform t)
        {
            SetTransform(Multiply(t, this));
        }

        public void PreConcatenate(EWAffineTransform t)
        {
            SetTransform(Multiply(this, t));
        }

        public EWAffineTransform CreateInverse()
        {
            float det = GetDeterminant();
            if (Math.Abs(det) < Zero)
            {
                // awt.204=Determinant is zero
                throw new Exception("Determinant is zero");
            }

            return new EWAffineTransform(
                _m11 / det, // m00
                -_m10 / det, // m10
                -_m01 / det, // m01
                _m00 / det, // m11
                (_m01 * _m12 - _m11 * _m02) / det, // m02
                (_m10 * _m02 - _m00 * _m12) / det // m12
            );
        }

        public EWPoint Transform(EWPoint src)
        {
            return Transform(src.X, src.Y);
        }

        public EWPoint Transform(float x, float y)
        {
            return new EWPoint(x * _m00 + y * _m01 + _m02, x * _m10 + y * _m11 + _m12);
        }

        public EWPoint InverseTransform(EWPoint src)
        {
            float det = GetDeterminant();
            if (Math.Abs(det) < Zero)
            {
                throw new Exception("Unable to inverse this transform.");
            }

            float x = src.X - _m02;
            float y = src.Y - _m12;

            return new EWPoint((x * _m11 - y * _m01) / det, (y * _m00 - x * _m10) / det);
        }

        public void Transform(float[] src, int srcOff, float[] dst, int dstOff, int length)
        {
            int step = 2;
            if (src == dst && srcOff < dstOff && dstOff < srcOff + length * 2)
            {
                srcOff = srcOff + length * 2 - 2;
                dstOff = dstOff + length * 2 - 2;
                step = -2;
            }

            while (--length >= 0)
            {
                float x = src[srcOff + 0];
                float y = src[srcOff + 1];
                dst[dstOff + 0] = x * _m00 + y * _m01 + _m02;
                dst[dstOff + 1] = x * _m10 + y * _m11 + _m12;
                srcOff += step;
                dstOff += step;
            }
        }

        public bool IsUnityTransform()
        {
            return !(HasScale() || HasRotate() || HasTranslate());
        }

        private bool HasScale()
        {
            // ReSharper disable CompareOfFloatsByEqualityOperator
            return _m00 != 1.0 || _m11 != 1.0;
            // ReSharper restore CompareOfFloatsByEqualityOperator
        }

        private bool HasRotate()
        {
            // ReSharper disable CompareOfFloatsByEqualityOperator
            return _m10 != 0.0 || _m01 != 0.0;
            // ReSharper restore CompareOfFloatsByEqualityOperator
        }

        private bool HasTranslate()
        {
            // ReSharper disable CompareOfFloatsByEqualityOperator
            return _m02 != 0.0 || _m12 != 0.0;
            // ReSharper restore CompareOfFloatsByEqualityOperator
        }

        public bool OnlyTranslate()
        {
            return !HasRotate() && !HasScale();
        }

        public bool OnlyTranslateOrScale()
        {
            return !HasRotate();
        }

        public bool OnlyScale()
        {
            return !HasRotate() && !HasTranslate();
        }

        public bool IsIdentity => (_m00 == 1.0f && _m11 == 1.0f) && (_m10 == 0.0f && _m01 == 0.0f && _m02 == 0.0f && _m12 == 0.0f);
    }
}