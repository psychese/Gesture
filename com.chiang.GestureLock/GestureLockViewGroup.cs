using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Graphics;
using Android.Content.Res;
using Android.Util;
using JavaMath = Java.Lang.Math;
using com.chiang.GestureLock;

namespace com.chiang.GestureLock {

    /**
 * 整体包含n*n个GestureLockView,每个GestureLockView间间隔mMarginBetweenLockView，
 * 最外层的GestureLockView与容器存在mMarginBetweenLockView的外边距
 * 
 * 关于GestureLockView的边长（n*n）： n * mGestureLockViewWidth + ( n + 1 ) *
 * mMarginBetweenLockView = mWidth ; 得：mGestureLockViewWidth = 4 * mWidth / ( 5
 * * mCount + 1 ) 注：mMarginBetweenLockView = mGestureLockViewWidth * 0.25 ; * 
 * 
 */
    public class GestureLockViewGroup : RelativeLayout {

        private static string TAG = "GestureLockViewGroup";

        /**
         * 保存所有的GestureLockView
         */
        private GestureLockView[] mGestureLockViews;
        /**
         * 每个边上的GestureLockView的个数
         */
        private int mCount = 4;
        /**
         * 存储答案
         */
        private List<int> mAnswer = new List<int>();
        /**
         * 保存用户选中的GestureLockView的id
         */
        private List<int> mChoose = new List<int>();

        private Paint mPaint;
        /**
         * 每个GestureLockView中间的间距 设置为：mGestureLockViewWidth * 25%
         */
        private int mMarginBetweenLockView = 30;
        /**
         * GestureLockView的边长 4 * mWidth / ( 5 * mCount + 1 )
         */
        private int mGestureLockViewWidth;

        /**
         * GestureLockView无手指触摸的状态下内圆的颜色FF939090
         */
        private int mNoFingerInnerCircleColor = 9670800;
        /**
         * GestureLockView无手指触摸的状态下外圆的颜色FFE0DBDB
         */
        private int mNoFingerOuterCircleColor = 14736347;
        /**
         * GestureLockView手指触摸的状态下内圆和外圆的颜色FF378FC9
         */
        private int mFingerOnColor = 3641289;
        /**
         * GestureLockView手指抬起的状态下内圆和外圆的颜色FFFF0000
         */
        private int mFingerUpColor = 16711680;

        /**
         * 宽度
         */
        private int mWidth;
        /**
         * 高度
         */
        private int mHeight;

        private Path mPath;
        /**
         * 指引线的开始位置x
         */
        private int mLastPathX;
        /**
         * 指引线的开始位置y
         */
        private int mLastPathY;
        /**
         * 指引下的结束位置
         */
        private Point mTmpTarget = new Point();

        /**
         * 最大尝试次数, 业务中尝试次数又外部逻辑控制
         */
        private int mTryTimes = 5;
        /// <summary>
        /// 手势输入完成
        /// </summary>
        public event Action<GestureCompletedArg> onGestureCompleted;

        public event Action<int> onPathPointPassed;

        private bool isShowGesturePath = true;

        public GestureLockViewGroup(Context context, IAttributeSet attrs)
            : this(context, attrs, 0) {
        }

        public void showGesturePath(bool isShow) {
            this.isShowGesturePath = isShow;
        }

        public GestureLockViewGroup(Context context, IAttributeSet attrs, int defStyle)
            : base(context, attrs, defStyle) {
            /**
             * 获得所有自定义的参数的值
             */
            TypedArray a = context.Theme.ObtainStyledAttributes(attrs, Resource.Styleable.GestureLockViewGroup, defStyle, 0);
            int n = a.IndexCount;

            for (int i = 0; i < n; i++) {
                int attr = a.GetIndex(i);
                if (attr == Resource.Styleable.GestureLockViewGroup_color_no_finger_inner_circle)
                    mNoFingerInnerCircleColor = a.GetColor(attr, mNoFingerInnerCircleColor);
                else if (attr == Resource.Styleable.GestureLockViewGroup_color_no_finger_outer_circle)
                    mNoFingerOuterCircleColor = a.GetColor(attr, mNoFingerOuterCircleColor);
                else if (attr == Resource.Styleable.GestureLockViewGroup_color_finger_on)
                    mFingerOnColor = a.GetColor(attr, mFingerOnColor);
                else if (attr == Resource.Styleable.GestureLockViewGroup_color_finger_up)
                    mFingerUpColor = a.GetColor(attr, mFingerUpColor);
                else if (attr == Resource.Styleable.GestureLockViewGroup_count)
                    mCount = a.GetInt(attr, 5);
                else if (attr == Resource.Styleable.GestureLockViewGroup_tryTimes)
                    mTryTimes = a.GetInt(attr, 0);
            }

            a.Recycle();
            a = null;

            // 初始化画笔
            mPaint = new Paint(PaintFlags.AntiAlias);
            mPaint.SetStyle(Paint.Style.Stroke);
            mPaint.StrokeWidth = 10;
            mPaint.StrokeCap = Paint.Cap.Round;
            mPaint.StrokeJoin = Paint.Join.Round;
            // mPaint.setColor(Color.parseColor("#aaffffff"));
            mPath = new Path();
        }

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec) {


            mWidth = MeasureSpec.GetSize(widthMeasureSpec);
            mHeight = MeasureSpec.GetSize(heightMeasureSpec);

            // Log.e(TAG, mWidth + "");
            // Log.e(TAG, mHeight + "");

            // 宽度和高度在layout中设置
            //mHeight = mWidth = mWidth < mHeight ? mWidth : mHeight;

            // setMeasuredDimension(mWidth, mHeight);

            // 初始化mGestureLockViews
            if (mGestureLockViews == null) {
                mGestureLockViews = new GestureLockView[mCount * mCount];
                // 计算每个GestureLockView的宽度
                //mGestureLockViewWidth = (int)(4 * mWidth * 1.0f / (5 * mCount + 1));
                var marginPercent = 0.8m;
                mGestureLockViewWidth = (int)(mWidth / mCount / (1 + marginPercent));
                var margin = mGestureLockViewWidth * mCount * marginPercent;
                //计算每个GestureLockView的间距
                mMarginBetweenLockView = (int)(margin / (mCount + 1));
                //mMarginBetweenLockView = (int)(mGestureLockViewWidth * mCount / (mCount + 1));
                // 设置画笔的宽度为GestureLockView的内圆直径稍微小点（不喜欢的话，随便设）
                //mPaint.StrokeWidth = mGestureLockViewWidth * 0.29f;

                for (int i = 0; i < mGestureLockViews.Length; i++) {
                    //初始化每个GestureLockView
                    mGestureLockViews[i] = new GestureLockView(this.Context,
                            mNoFingerInnerCircleColor, mNoFingerOuterCircleColor,
                            mFingerOnColor, mFingerUpColor);
                    mGestureLockViews[i].Id = i + 1;
                    //设置参数，主要是定位GestureLockView间的位置
                    RelativeLayout.LayoutParams lockerParams = new RelativeLayout.LayoutParams(
                            mGestureLockViewWidth, mGestureLockViewWidth);

                    // 不是每行的第一个，则设置位置为前一个的右边
                    if (i % mCount != 0) {
                        lockerParams.AddRule(LayoutRules.RightOf,
                                mGestureLockViews[i - 1].Id);
                    }
                    // 从第二行开始，设置为上一行同一位置View的下面
                    if (i > mCount - 1) {
                        lockerParams.AddRule(LayoutRules.Below,
                                mGestureLockViews[i - mCount].Id);
                    }
                    //设置右下左上的边距
                    int rightMargin = mMarginBetweenLockView;
                    int bottomMargin = mMarginBetweenLockView;
                    int leftMagin = 0;
                    int topMargin = 0;
                    /**
                     * 每个View都有右外边距和底外边距 第一行的有上外边距 第一列的有左外边距
                     */
                    if (i >= 0 && i < mCount) {// 第一行
                        topMargin = mMarginBetweenLockView;
                    }
                    if (i % mCount == 0) {// 第一列
                        leftMagin = mMarginBetweenLockView;
                    }

                    lockerParams.SetMargins(leftMagin, topMargin, rightMargin, bottomMargin);
                    mGestureLockViews[i].setMode(GestureLockView.Mode.STATUS_NO_FINGER);
                    AddView(mGestureLockViews[i], lockerParams);
                }

                Log.Error(TAG, "mWidth = " + mWidth + " ,  mGestureViewWidth = "
                        + mGestureLockViewWidth + " , mMarginBetweenLockView = "
                        + mMarginBetweenLockView);

            }

            //此方法调用后将触发groupview的draw方法
            //在此代码之前必须计算子控件的摆放位置，否则较低版本的sdk将无法正常绘制手势
            base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
        }

        public override bool OnTouchEvent(MotionEvent e) {
            var action = e.Action;
            int x = (int)e.GetX();
            int y = (int)e.GetY();

            switch (action) {
                case MotionEventActions.Down:
                    // 重置
                    reset();
                    break;
                case MotionEventActions.Move:
                    mPaint.Color = new Color(mFingerOnColor);
                    //mPaint.Alpha = 50;
                    GestureLockView child = getChildIdByPos(x, y);
                    if (child != null) {
                        int cId = child.Id;
                        if (!mChoose.Contains(cId)) {
                            mChoose.Add(cId);
                            if (isShowGesturePath) {
                                child.setMode(GestureLockView.Mode.STATUS_FINGER_ON);
                                if (onPathPointPassed != null) onPathPointPassed(cId);
                                // 设置指引线的起点
                                mLastPathX = child.Left / 2 + child.Right / 2;
                                mLastPathY = child.Top / 2 + child.Bottom / 2;

                                if (mChoose.Count == 1) {
                                    // 当前添加为第一个
                                    mPath.MoveTo(mLastPathX, mLastPathY);
                                }
                                else {
                                    // 非第一个，将两者使用线连上
                                    mPath.LineTo(mLastPathX, mLastPathY);
                                }
                            }
                            else {
                                child.setMode(GestureLockView.Mode.STATUS_NO_FINGER);
                            }
                        }
                    }
                    // 指引线的终点
                    mTmpTarget.X = x;
                    mTmpTarget.Y = y;
                    break;
                case MotionEventActions.Up:
                    mPaint.Color = new Color(mFingerUpColor);
                    //mPaint.Alpha = 50;

                    if (this.mTryTimes > 0) {
                        if (this.mAnswer.Count > 0)
                            this.mTryTimes--;

                        //回调
                        if (onGestureCompleted != null && mChoose.Count > 0) {
                            if (this.mAnswer != null && this.mAnswer.Count > 0)
                                onGestureCompleted(new GestureCompletedArg {
                                    result = this.mChoose.ToArray(),
                                    matched = checkAnswer(),
                                    remainTryTimes = this.mTryTimes,
                                    outTryTime = this.mTryTimes == 0,
                                });
                            else
                                onGestureCompleted(new GestureCompletedArg {
                                    result = this.mChoose.ToArray(),
                                });
                        }

                        Log.Error(TAG, "mUnMatchExceedBoundary = " + mTryTimes);
                        Log.Error(TAG, "mChoose = " + mChoose);
                        // 将终点设置位置为起点，即取消指引线
                        mTmpTarget.X = mLastPathX;
                        mTmpTarget.Y = mLastPathY;

                        // 改变子元素的状态为UP
                        //changeItemMode();

                        // 计算每个元素中箭头需要旋转的角度
                        for (int i = 0; i + 1 < mChoose.Count; i++) {
                            int childId = mChoose[i];
                            int nextChildId = mChoose[i + 1];

                            GestureLockView startChild = FindViewById<GestureLockView>(childId);
                            GestureLockView nextChild = FindViewById<GestureLockView>(nextChildId);

                            int dx = nextChild.Left - startChild.Left;
                            int dy = nextChild.Top - startChild.Top;

                            // 计算角度
                            int angle = (int)JavaMath.ToDegrees(JavaMath.Atan2(dy, dx)) + 90;
                            startChild.setArrowDegree(angle);
                        }
                    }
                    touchOverClearPath();
                    break;

            }
            Invalidate();
            return true;
        }

        private void changeItemMode() {
            foreach (GestureLockView gestureLockView in mGestureLockViews) {
                if (mChoose.Contains(gestureLockView.Id)) {
                    gestureLockView.setMode(GestureLockView.Mode.STATUS_FINGER_UP);
                }
            }
        }

        /**
         * 
         * 做一些必要的重置
         */
        private void reset() {
            mChoose.Clear();
            mPath.Reset();
            foreach (GestureLockView gestureLockView in mGestureLockViews) {
                gestureLockView.setMode(GestureLockView.Mode.STATUS_NO_FINGER);
                gestureLockView.setArrowDegree(-1);
            }
        }
        /*
         * 
         * 触摸完毕之后清除触摸轨迹
         */
        private void touchOverClearPath() {
            mPath.Reset();
            foreach (GestureLockView gestureLockView in mGestureLockViews) {
                gestureLockView.setMode(GestureLockView.Mode.STATUS_NO_FINGER);
                gestureLockView.setArrowDegree(-1);
            }
        }

        /**
         * 检查用户绘制的手势是否正确
         * @return
         */
        private bool checkAnswer() {
            if (mAnswer.Count != mChoose.Count)
                return false;

            for (int i = 0; i < mAnswer.Count; i++) {
                if (mAnswer[i] != mChoose[i])
                    return false;
            }

            return true;
        }

        /**
         * 检查当前左边是否在child中
         * @param child
         * @param x
         * @param y
         * @return
         */
        private bool checkPositionInChild(View child, int x, int y) {

            //设置了内边距，即x,y必须落入下GestureLockView的内部中间的小区域中，可以通过调整padding使得x,y落入范围不变大，或者不设置padding
            int padding = (int)(mGestureLockViewWidth * 0.15);

            if (x >= child.Left + padding && x <= child.Right - padding
                    && y >= child.Top + padding && y <= child.Bottom - padding) {
                return true;
            }

            return false;
        }

        /**
         * 通过x,y获得落入的GestureLockView
         * @param x
         * @param y
         * @return
         */
        private GestureLockView getChildIdByPos(int x, int y) {
            foreach (GestureLockView gestureLockView in mGestureLockViews) {
                if (checkPositionInChild(gestureLockView, x, y)) {
                    return gestureLockView;
                }
            }

            return null;

        }

        /// <summary>
        /// 设置默认值，用以对比输入的手势
        /// </summary>
        /// <param name="defAnswer"></param>
        public void setDefAnswer(int[] defAnswer) {
            this.mAnswer = defAnswer.ToList();
        }

        /// <summary>
        /// 获取手势输入结果
        /// </summary>
        /// <returns></returns>
        public int[] getGesture() {
            return this.mChoose.ToArray();
        }

        /// <summary>
        /// 设置手势对比最大次数
        /// </summary>
        /// <param name="boundary"></param>
        public void setUnMatchExceedBoundary(int boundary) {
            this.mTryTimes = boundary;
        }

        protected override void DispatchDraw(Canvas canvas) {
            base.DispatchDraw(canvas);

            //绘制GestureLockView间的连线
            if (mPath != null) {
                canvas.DrawPath(mPath, mPaint);
            }
            //绘制指引线
            if (mChoose.Count > 0) {
                if (mLastPathX != 0 && mLastPathY != 0) {
                    //canvas.DrawLine(mLastPathX, mLastPathY, mTmpTarget.X,
                    //        mTmpTarget.Y, mPaint);

                    // 改为绘制path后，绘制线条为圆角
                    using (var tmpPath = new Path()) {
                        tmpPath.MoveTo(mLastPathX, mLastPathY);
                        tmpPath.LineTo(mTmpTarget.X, mTmpTarget.Y);
                        canvas.DrawPath(tmpPath, mPaint);
                    }
                }
            }
        }

        public class BlockSelectedArg {
            public int cId { get; set; }
        }

        public class GestureCompletedArg {
            public int[] result { get; set; }
            public bool? matched { get; set; }
            public int remainTryTimes { get; set; }
            public bool? outTryTime { get; set; }
        }
    }
}