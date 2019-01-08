﻿using System;
using Unity.UIWidgets.foundation;
using Unity.UIWidgets.gestures;
using Unity.UIWidgets.painting;
using Unity.UIWidgets.ui;
using UnityEngine;
using Rect = Unity.UIWidgets.ui.Rect;

namespace Unity.UIWidgets.rendering {
    public class ViewConfiguration {
        public ViewConfiguration(
            Size size = null,
            double devicePixelRatio = 1.0
        ) {
            this.size = size ?? Size.zero;
            this.devicePixelRatio = devicePixelRatio;
        }

        public readonly Size size;

        public readonly double devicePixelRatio;

        public Matrix3 toMatrix() {
            return Matrix3.I();
        }

        public override string ToString() {
            return string.Format("${0} at ${1}x", this.size, this.devicePixelRatio);
        }
    }

    public class RenderView : RenderObjectWithChildMixinRenderObject<RenderBox> {
        public RenderView(
            RenderBox child = null,
            ViewConfiguration configuration = null) {
            D.assert(configuration != null);

            this.child = child;
            this._configuration = configuration;
        }

        public Size size {
            get { return this._size; }
        }

        Size _size = Size.zero;

        public ViewConfiguration configuration {
            get { return this._configuration; }
            set {
                D.assert(value != null);
                if (value == this._configuration) {
                    return;
                }

                this._configuration = value;
                this.replaceRootLayer((OffsetLayer) this._updateMatricesAndCreateNewRootLayer());
                this.markNeedsLayout();
            }
        }

        ViewConfiguration _configuration;

        public void scheduleInitialFrame() {
            D.assert(this.owner != null);
            this.scheduleInitialLayout();
            this.scheduleInitialPaint((OffsetLayer) this._updateMatricesAndCreateNewRootLayer());
            this.owner.requestVisualUpdate();
        }

        Matrix3 _rootTransform;

        public Layer _updateMatricesAndCreateNewRootLayer() {
            this._rootTransform = this.configuration.toMatrix();
            ContainerLayer rootLayer = new TransformLayer(transform: this._rootTransform);
            rootLayer.attach(this);
            return rootLayer;
        }

        protected override void debugAssertDoesMeetConstraints() {
            D.assert(false);
        }

        protected override void performResize() {
            D.assert(false);
        }

        protected override void performLayout() {
            this._size = this.configuration.size;
            D.assert(this._size.isFinite);

            if (this.child != null) {
                this.child.layout(BoxConstraints.tight(this._size));
            }
        }

        public bool hitTest(HitTestResult result, Offset position = null) {
            if (this.child != null) {
                this.child.hitTest(result, position: position);
            }

            result.add(new HitTestEntry(this));
            return true;
        }

        public override bool isRepaintBoundary {
            get { return true; }
        }

        public override void paint(PaintingContext context, Offset offset) {
            if (this.child != null) {
                context.paintChild(this.child, offset);
            }
        }

        public override void applyPaintTransform(RenderObject child, ref Matrix3 transform) {
            transform *= this._rootTransform;
            base.applyPaintTransform(child, ref transform);
        }

        public void compositeFrame() {
            var builder = new SceneBuilder();
            this.layer.addToScene(builder, Offset.zero);
            using (var scene = builder.build()) {
                Window.instance.render(scene);
            }

            D.assert(() => {
//                if (D.debugRepaintRainbowEnabled || D.debugRepaintTextRainbowEnabled) {
//                    debugCurrentRepaintColor =
//                        debugCurrentRepaintColor.withHue((debugCurrentRepaintColor.hue + 2.0) % 360.0);
//                }

                return true;
            });
        }

        public override Rect paintBounds {
            get { return Offset.zero & (this.size * this.configuration.devicePixelRatio); }
        }

        public override Rect semanticBounds {
            get
            {
                D.assert(_rootTransform != null);
                return _rootTransform.mapRect(Offset.zero & size);
            }
        }
        
        public override void debugFillProperties(DiagnosticPropertiesBuilder properties) {
            D.assert(() => {
                properties.add(DiagnosticsNode.message("debug mode enabled"));
                return true;
            });
            properties.add(new DiagnosticsProperty<Size>("window size", Window.instance.physicalSize,
                tooltip: "in physical pixels"));
            properties.add(new DoubleProperty("device pixel ratio", Window.instance.devicePixelRatio,
                tooltip: "physical pixels per logical pixel"));
            properties.add(new DiagnosticsProperty<ViewConfiguration>("configuration", this.configuration,
                tooltip: "in logical pixels"));
        }
    }
}