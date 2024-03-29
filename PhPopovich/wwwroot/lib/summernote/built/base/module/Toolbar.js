"use strict";
exports.__esModule = true;
var jquery_1 = require("jquery");
var Toolbar = /** @class */ (function () {
    function Toolbar(context) {
        this.context = context;
        this.$window = jquery_1["default"](window);
        this.$document = jquery_1["default"](document);
        this.ui = jquery_1["default"].summernote.ui;
        this.$note = context.layoutInfo.note;
        this.$editor = context.layoutInfo.editor;
        this.$toolbar = context.layoutInfo.toolbar;
        this.$editable = context.layoutInfo.editable;
        this.$statusbar = context.layoutInfo.statusbar;
        this.options = context.options;
        this.isFollowing = false;
        this.followScroll = this.followScroll.bind(this);
    }
    Toolbar.prototype.shouldInitialize = function () {
        return !this.options.airMode;
    };
    Toolbar.prototype.initialize = function () {
        var _this = this;
        this.options.toolbar = this.options.toolbar || [];
        if (!this.options.toolbar.length) {
            this.$toolbar.hide();
        }
        else {
            this.context.invoke('buttons.build', this.$toolbar, this.options.toolbar);
        }
        if (this.options.toolbarContainer) {
            this.$toolbar.appendTo(this.options.toolbarContainer);
        }
        this.changeContainer(false);
        this.$note.on('summernote.keyup summernote.mouseup summernote.change', function () {
            _this.context.invoke('buttons.updateCurrentStyle');
        });
        this.context.invoke('buttons.updateCurrentStyle');
        if (this.options.followingToolbar) {
            this.$window.on('scroll resize', this.followScroll);
        }
    };
    Toolbar.prototype.destroy = function () {
        this.$toolbar.children().remove();
        if (this.options.followingToolbar) {
            this.$window.off('scroll resize', this.followScroll);
        }
    };
    Toolbar.prototype.followScroll = function () {
        if (this.$editor.hasClass('fullscreen')) {
            return false;
        }
        var editorHeight = this.$editor.outerHeight();
        var editorWidth = this.$editor.width();
        var toolbarHeight = this.$toolbar.height();
        var statusbarHeight = this.$statusbar.height();
        // check if the web app is currently using another static bar
        var otherBarHeight = 0;
        if (this.options.otherStaticBar) {
            otherBarHeight = jquery_1["default"](this.options.otherStaticBar).outerHeight();
        }
        var currentOffset = this.$document.scrollTop();
        var editorOffsetTop = this.$editor.offset().top;
        var editorOffsetBottom = editorOffsetTop + editorHeight;
        var activateOffset = editorOffsetTop - otherBarHeight;
        var deactivateOffsetBottom = editorOffsetBottom - otherBarHeight - toolbarHeight - statusbarHeight;
        if (!this.isFollowing &&
            (currentOffset > activateOffset) && (currentOffset < deactivateOffsetBottom - toolbarHeight)) {
            this.isFollowing = true;
            this.$toolbar.css({
                position: 'fixed',
                top: otherBarHeight,
                width: editorWidth,
                zIndex: 1000
            });
            this.$editable.css({
                marginTop: this.$toolbar.height() + 5
            });
        }
        else if (this.isFollowing &&
            ((currentOffset < activateOffset) || (currentOffset > deactivateOffsetBottom))) {
            this.isFollowing = false;
            this.$toolbar.css({
                position: 'relative',
                top: 0,
                width: '100%',
                zIndex: 'auto'
            });
            this.$editable.css({
                marginTop: ''
            });
        }
    };
    Toolbar.prototype.changeContainer = function (isFullscreen) {
        if (isFullscreen) {
            this.$toolbar.prependTo(this.$editor);
        }
        else {
            if (this.options.toolbarContainer) {
                this.$toolbar.appendTo(this.options.toolbarContainer);
            }
        }
        if (this.options.followingToolbar) {
            this.followScroll();
        }
    };
    Toolbar.prototype.updateFullscreen = function (isFullscreen) {
        this.ui.toggleBtnActive(this.$toolbar.find('.btn-fullscreen'), isFullscreen);
        this.changeContainer(isFullscreen);
    };
    Toolbar.prototype.updateCodeview = function (isCodeview) {
        this.ui.toggleBtnActive(this.$toolbar.find('.btn-codeview'), isCodeview);
        if (isCodeview) {
            this.deactivate();
        }
        else {
            this.activate();
        }
    };
    Toolbar.prototype.activate = function (isIncludeCodeview) {
        var $btn = this.$toolbar.find('button');
        if (!isIncludeCodeview) {
            $btn = $btn.not('.btn-codeview').not('.btn-fullscreen');
        }
        this.ui.toggleBtn($btn, true);
    };
    Toolbar.prototype.deactivate = function (isIncludeCodeview) {
        var $btn = this.$toolbar.find('button');
        if (!isIncludeCodeview) {
            $btn = $btn.not('.btn-codeview').not('.btn-fullscreen');
        }
        this.ui.toggleBtn($btn, false);
    };
    return Toolbar;
}());
exports["default"] = Toolbar;
//# sourceMappingURL=Toolbar.js.map