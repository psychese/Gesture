using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Provider;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Graphics;
using Android.Util;
using Android.Content.Res;
using com.chiang.GestureLock;

namespace com.chiang.GestureLock {

    public class GestureLockThumbnail : RelativeLayout {
        private static readonly int defCount = 5;
        private int mCount;
        private float radius;
        private List<PointF> shapePositions = new List<PointF>();
        private List<int> results = new List<int>();

        private Paint paint;

        public GestureLockThumbnail(Context context, IAttributeSet attrs)
            : this(context, attrs, 0) {
        }

        public GestureLockThumbnail(Context context, IAttributeSet attrs, int defStyle)
            : base(context, attrs, defStyle) {
            TypedArray a = context.Theme.ObtainStyledAttributes(attrs, Resource.Styleable.GestureLockThumbnail, defStyle, 0);
            int n = a.IndexCount;

            for (int i = 0; i < n; i++) {
                int attr = a.GetIndex(i);
                if (attr == Resource.Styleable.GestureLockThumbnail_count)
                    mCount = a.GetInt(attr, defCount);
            }

            paint = new Paint(PaintFlags.AntiAlias);
            paint.SetStyle(Paint.Style.FillAndStroke);
            paint.Color = Color.White;
            paint.StrokeWidth = 2;
        }

        protected override void DispatchDraw(Canvas canvas) {
            base.DispatchDraw(canvas);
            for (int i = 0; i < shapePositions.Count; i++) {

                var p = shapePositions[i];
                //GestureLockViewGroup id从1开始
                if (results.Contains(i + 1)) {
                    paint.Alpha = 200;
                }
                else {
                    paint.Alpha = 20;
                }
                canvas.DrawCircle(p.X, p.Y, radius, paint);
            }
        }

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec) {
            base.OnMeasure(widthMeasureSpec, heightMeasureSpec);

            var mWidth = MeasureSpec.GetSize(widthMeasureSpec);
            var mHeight = MeasureSpec.GetSize(heightMeasureSpec);

            // Log.e(TAG, mWidth + "");
            // Log.e(TAG, mHeight + "");

            mHeight = mWidth = mWidth < mHeight ? mWidth : mHeight;

            var sharps = mCount * mCount;

            // 圆形的宽度
            var mGestureLockViewWidth = mWidth / (mCount + 2.0f); //(4 * mWidth * 1.0f / (5 * mCount + 1.0f));
            // 每个圆形的间距
            var mMarginBetweenLockView = (mGestureLockViewWidth * 2.0f) / (mCount + 1.0f);
            radius = mGestureLockViewWidth / 2;

            for (int i = 0; i < mCount; i++) {
                for (int k = 0; k < mCount; k++) {
                    var x = (k + 1) * mMarginBetweenLockView + (k * 2 + 1) * radius;
                    var y = (i + 1) * mMarginBetweenLockView + (i * 2 + 1) * radius;
                    shapePositions.Add(new PointF(x, y));
                }
            }
        }

        public void SetReslut(int[] results) {
            this.results = results.ToList();
            Invalidate();
        }
    }
}
