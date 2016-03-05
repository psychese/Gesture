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

namespace com.chiang.GestureLock {

    public class GestureLockView : View {
        private static string TAG = "GestureLockView";
        /**
         * GestureLockView的三种状态
         */
        public enum Mode {
            STATUS_NO_FINGER, STATUS_FINGER_ON, STATUS_FINGER_UP
        }

        /**
         * GestureLockView的当前状态
         */
        private Mode mCurrentStatus = Mode.STATUS_NO_FINGER;

        /**
         * 宽度
         */
        private int mWidth;
        /**
         * 高度
         */
        private int mHeight;
        /**
         * 外圆半径
         */
        private int mRadius;
        /**
         * 画笔的宽度
         */
        private int mStrokeWidth = 2;

        /**
         * 圆心坐标
         */
        private int mCenterX;
        private int mCenterY;
        private Paint mPaint;

        /**
         * 箭头（小三角最长边的一半长度 = mArrawRate * mWidth / 2 ）
         */
        private float mArrowRate = 0.333f;
        private int mArrowDegree = -1;
        private Path mArrowPath;
        /**
         * 内圆的半径 = mInnerCircleRadiusRate * mRadus
         * 
         */
        private float mInnerCircleRadiusRate = 0.3F;

        /**
         * 四个颜色，可由用户自定义，初始化时由GestureLockViewGroup传入
         */
        private int mColorNoFingerInner;
        private int mColorNoFingerOutter;
        private int mColorFingerOn;
        private int mColorFingerUp;

        public GestureLockView(Context context, int colorNoFingerInner, int colorNoFingerOutter, int colorFingerOn, int colorFingerUp)
            : base(context) {

            this.mColorNoFingerInner = colorNoFingerInner;
            this.mColorNoFingerOutter = colorNoFingerOutter;
            this.mColorFingerOn = colorFingerOn;
            this.mColorFingerUp = colorFingerUp;
            mPaint = new Paint(PaintFlags.AntiAlias);
            mArrowPath = new Path();

        }

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec) {
            base.OnMeasure(widthMeasureSpec, heightMeasureSpec);

            mWidth = MeasureSpec.GetSize(widthMeasureSpec);
            mHeight = MeasureSpec.GetSize(heightMeasureSpec);

            // 取长和宽中的小值
            mWidth = mWidth < mHeight ? mWidth : mHeight;
            mRadius = mCenterX = mCenterY = mWidth / 2;
            mRadius -= mStrokeWidth / 2;

            // 绘制三角形，初始时是个默认箭头朝上的一个等腰三角形，用户绘制结束后，根据由两个GestureLockView决定需要旋转多少度
            float mArrowLength = mWidth / 2 * mArrowRate;
            mArrowPath.MoveTo(mWidth / 2, mStrokeWidth + 2);
            mArrowPath.LineTo(mWidth / 2 - mArrowLength, mStrokeWidth + 2
                    + mArrowLength);
            mArrowPath.LineTo(mWidth / 2 + mArrowLength, mStrokeWidth + 2
                    + mArrowLength);
            mArrowPath.Close();
            mArrowPath.SetFillType(Path.FillType.Winding);
        }

        protected override void OnDraw(Canvas canvas) {

            switch (mCurrentStatus) {
                case Mode.STATUS_FINGER_ON:

                    // 绘制外圆
                    mPaint.SetStyle(Paint.Style.Stroke);
                    mPaint.Color = new Color(mColorFingerOn);
                    mPaint.StrokeWidth = 2;
                    canvas.DrawCircle(mCenterX, mCenterY, mRadius, mPaint);

                    // 绘制外圆遮蔽层
                    mPaint.SetStyle(Paint.Style.Fill);
                    var transparentColor = new Color(mColorFingerOn) { A = 64 };
                    mPaint.Color = new Color(transparentColor);
                    canvas.DrawCircle(mCenterX, mCenterY, mRadius, mPaint);

                    // 绘制内圆
                    mPaint.SetStyle(Paint.Style.Fill);
                    mPaint.Color = new Color(mColorFingerOn);
                    canvas.DrawCircle(mCenterX, mCenterY, mRadius
                            * mInnerCircleRadiusRate, mPaint);
                    break;
                case Mode.STATUS_FINGER_UP:
                    // 绘制外圆
                    mPaint.Color = new Color(mColorFingerUp);
                    mPaint.SetStyle(Paint.Style.Stroke);
                    mPaint.StrokeWidth = 2;
                    canvas.DrawCircle(mCenterX, mCenterY, mRadius, mPaint);
                    // 绘制内圆
                    mPaint.SetStyle(Paint.Style.Fill);
                    canvas.DrawCircle(mCenterX, mCenterY, mRadius
                            * mInnerCircleRadiusRate, mPaint);

                    drawArrow(canvas);

                    break;

                case Mode.STATUS_NO_FINGER:

                    //// 绘制外圆
                    //mPaint.SetStyle(Paint.Style.Fill);
                    //mPaint.Color = new Color(mColorNoFingerOutter);
                    //canvas.DrawCircle(mCenterX, mCenterY, mRadius, mPaint);
                    //// 绘制内圆
                    //mPaint.Color = new Color(mColorNoFingerInner);
                    //canvas.DrawCircle(mCenterX, mCenterY, mRadius
                    //        * mInnerCircleRadiusRate, mPaint);

                    // 绘制圆环
                    mPaint.Color = new Color(mColorNoFingerOutter);
                    mPaint.SetStyle(Paint.Style.Stroke);
                    mPaint.StrokeWidth = 2;
                    canvas.DrawCircle(mCenterX, mCenterY, mRadius, mPaint);

                    break;
                default:
                    break;
            }
        }

        /**
         * 绘制箭头
         * @param canvas
         */
        private void drawArrow(Canvas canvas) {
            if (mArrowDegree != -1) {
                mPaint.SetStyle(Paint.Style.Fill);

                canvas.Save();
                canvas.Rotate(mArrowDegree, mCenterX, mCenterY);
                canvas.DrawPath(mArrowPath, mPaint);

                canvas.Restore();
            }

        }

        /**
         * 设置当前模式并重绘界面
         * 
         * @param mode
         */
        public void setMode(Mode mode) {
            this.mCurrentStatus = mode;
            Invalidate();
        }

        public Mode getMode() {
            return this.mCurrentStatus;
        }

        public void setArrowDegree(int degree) {
            this.mArrowDegree = degree;
        }

        public int getArrowDegree() {
            return this.mArrowDegree;
        }
    }
}