/* ===================================== *\
    Controllers
\* ===================================== */

angular.module('umbraco').controller("Our.Umbraco.PropertyEditors.PhotonController",
    function ($rootScope, $routeParams, $scope, entityResource) {

        var config = angular.copy($scope.model.config);

        function setupViewModel() {
            console.log("setupViewModel");
            console.log($scope.model.value);

            $scope.imageSrc = undefined;
            $scope.imageId = 0;
            $scope.tags = [];

            if ($scope.model.value && $scope.model.value.imageId > 0) {
                entityResource.getById($scope.model.value.imageId, "Media").then(function (media) {

                    // Extract media url
                    var src = media.hasOwnProperty("metaData")
                        ? media.metaData["umbracoFile"] : "";

                    // Check for image cropper
                    if (typeof src === 'object' && src.hasOwnProperty("PropertyEditorAlias") && src["PropertyEditorAlias"] == "Umbraco.ImageCropper") {
                        src = src.Value.src;
                    }

                    // Src is some other object so set to empty string
                    if (typeof src === 'object') {
                        src = "";
                    }

                    // Update scope
                    $scope.imageSrc = src;
                    $scope.imageId = media.id;
                    $scope.tags = $scope.model.value.tags;
                    $scope.sync();
                });
            }
        }

        setupViewModel();

        $scope.showAdd = function () {
            return !$scope.imageSrc;
        };

        $scope.add = function () {

            $scope.mediaPickerOverlay = {
                view: "mediapicker",
                title: "Select media",
                multiPicker: false,
                show: true,
                submit: function (model) {

                    if (model.selectedImages.length > 0 && model.selectedImages[0].hasOwnProperty("image")) {
                        $scope.imageSrc = model.selectedImages[0].image;
                        $scope.imageId = model.selectedImages[0].id;
                    }
                    
                    $scope.sync();

                    $scope.mediaPickerOverlay.show = false;
                    $scope.mediaPickerOverlay = null;

                }
            };

        };

        $scope.clear = function () {
            if (confirm("You sure?")) {
                $scope.imageSrc = undefined;
                $scope.imageId = 0;
                $scope.tags = [];
                $scope.sync();
            }
        };

        $scope.sync = function () {
            $scope.model.value = $scope.imageId > 0
                ? { imageId: $scope.imageId, tags: $scope.tags }
                : undefined;
        };

        //here we declare a special method which will be called whenever the value has changed from the server
        $scope.model.onValueChanged = function (newVal, oldVal) {
            setupViewModel();
        };
    });

angular.module("umbraco").controller("Our.Umbraco.Dialogs.PhotonMetaDataController",
    [
        "$scope",
        "editorState",
        "contentResource",

        function ($scope, editorState, contentResource) {

            $scope.node = null;

            $scope.save = function () {

                // DO NOT REMOVE
                // Fixes issue with links not updating tiny mce value.
                // We need to force an execCommand to run to make it
                // call the handler that updates the model value.
                if ("tinyMCE" in window) {
                    for (var edId in tinyMCE.editors) {
                        tinyMCE.editors[edId].execCommand('mceRepaint');
                    }
                }

                if (!$scope.photonForm.$valid)
                    return;

                // Copy property values to scope model value
                if ($scope.node) {
                    var value = {};
                    for (var t = 0; t < $scope.node.tabs.length; t++) {
                        var tab = $scope.node.tabs[t];
                        for (var p = 0; p < tab.properties.length; p++) {
                            var prop = tab.properties[p];
                            if (typeof prop.value !== "function") {
                                value[prop.alias] = prop.value;
                            }
                        }
                    }
                    $scope.dialogData.value = value;
                } else {
                    $scope.dialogData.value = null;
                }

                $scope.submit($scope.dialogData);
            };

            function loadNode() {
                contentResource.getScaffold(-20, $scope.dialogData.metaDataDocType).then(function (data) {
                    // Remove the last tab
                    data.tabs.pop();

                    // Merge current value
                    if ($scope.dialogData.value) {
                        //$scope.nameProperty.value = $scope.dialogData.value.name;
                        for (var t = 0; t < data.tabs.length; t++) {
                            var tab = data.tabs[t];
                            for (var p = 0; p < tab.properties.length; p++) {
                                var prop = tab.properties[p];
                                if ($scope.dialogData.value[prop.alias]) {
                                    prop.value = $scope.dialogData.value[prop.alias];
                                }
                            }
                        }
                    };

                    // Assign the model to scope
                    $scope.node = data;

                    editorState.set($scope.node);
                });
            };

            loadNode();
        }

    ]);

/* ===================================== *\
    Directives
\* ===================================== */

angular.module("umbraco.directives").directive('photonImage',
[
    "Our.Umbraco.Services.photonMetaDataDialogService",
    function (metaDataDialogService) {

        var origImageWidth, origImageHeight, imageWidth, imageHeight, ias;

        var updateImageDimensions = function (url, imgWidth) {
            var tmpImg = new Image();
            tmpImg.src = url;
            $(tmpImg).one('load', function () {
                origImageWidth = tmpImg.width;
                origImageHeight = tmpImg.height;

                imageWidth = imgWidth;
                imageHeight = Math.round((imageWidth / origImageWidth) * origImageHeight);

                delete tmpImg;
            });
        }

        var percToPx = function (val, maxVal) {
            return (maxVal / 100) * val;
        };

        var pxToPerc = function (val, maxVal) {
            return (100 / maxVal) * val;
        };

        var link = function ($scope, element, attrs, ctrl) {

            $scope.model.value.tags = $scope.model.value.tags || [];
            $scope.currentTag = null;

            var initImgAreaSelect = function () {
                ias = $(element).find(".photon-image").imgAreaSelect({
                    hide: true,
                    handles: false,
                    instance: true,
                    parent: $(element).find(".photon-image-bounds"),
                    onSelectStart: function (img) {
                        $scope.$apply(function () {
                            $scope.currentTag = null;
                        });
                        ias.setOptions({ show: true });
                        ias.update();
                    },
                    onSelectEnd: function (img, sel) {

                        var tooSmall = sel.width == 0 || sel.height == 0;
                        if ($scope.currentTag == null && tooSmall)
                            return;

                        var tag = $scope.currentTag || { id: guid(), metaData: {} };

                        tag.x = pxToPerc(sel.x1, imageWidth);
                        tag.y = pxToPerc(sel.y1, imageHeight);
                        tag.width = pxToPerc(sel.width, imageWidth);
                        tag.height = pxToPerc(sel.height, imageHeight);

                        if ($scope.currentTag == null) {
                            $scope.$apply(function() {
                                $scope.model.value.tags.push(tag);
                                $scope.currentTag = tag;
                            });
                        } else {
                            $scope.$apply();
                        };
                    }
                });
            }

            $scope.isCurrentTag = function (tag) {
                var isCurrentTag = $scope.currentTag != null && tag.id == $scope.currentTag.id;
                return isCurrentTag;
            }

            $scope.deselectCurrentTag = function (e) {
                $scope.currentTag = null;
            }

            $scope.selectTag = function (tag, e) {
                e.stopPropagation();
                $scope.currentTag = tag;
            }

            $scope.editTag = function (tag) {
                metaDataDialogService.open({
                    dialogData: {
                        metaDataDocType: $scope.model.config.metaDataDocType,
                        value: tag.metaData
                    },
                    callback: function (data) {
                        tag.metaData = data.value;
                    }
                });
            }

            $scope.deleteTag = function (tag) {
                $scope.model.value.tags = $.grep($scope.model.value.tags, function (itm, idx) {
                    return tag.id !== itm.id;
                });
                if ($scope.isCurrentTag(tag)) {
                    $scope.currentTag = null;
                }
            }

            $scope.$watch('src', function (newValue, oldValue) {
                if (newValue != oldValue) {
                    $scope.model.value.tags = [];
                    if (ias != undefined) {
                        ias.update({ remove: true });
                    }
                }
                if (newValue) {
                    updateImageDimensions(newValue, $scope.model.config.imageWidth);
                    initImgAreaSelect();
                }
            });

            $scope.$watch('currentTag', function (newValue, oldValue) {
                if (newValue != null) {
                    if (newValue != oldValue) {
                        ias.setSelection(percToPx(newValue.x, imageWidth),
                            percToPx(newValue.y, imageHeight),
                            percToPx(newValue.x + newValue.width, imageWidth),
                            percToPx(newValue.y + newValue.height, imageHeight));
                        ias.setOptions({ show: true });
                        ias.update();
                    }
                } else {
                    if (newValue != oldValue) {
                        ias.setOptions({ hide: true });
                        ias.update();
                    }
                }
            });

            $scope.$on("formSubmitting", function () {
                $scope.deselectCurrentTag();
            });

            $scope.$on('$destroy', function () {
                ias.setOptions({ remove: true });
                $scope.deselectCurrentTag();
            });
        }

        return {
            restrict: "E",
            replace: true,
            template: "<div>" +
                    "<div class='photon-image-bounds' style=\"background-color:{{model.config.backgroundColor}};\">" +
                        "<div class='photon-tag' ng-repeat=\"tag in model.value.tags\" ng-class=\"{active:tag.id==currentTag.id}\" ng-mousedown=\"selectTag(tag, $event);\" style=\"width:{{tag.width}}%;height:{{tag.height}}%;left:{{tag.x}}%;top:{{tag.y}}%;\">" +
                            "<div class='photon-tag-tools'>" +
                                "<span class=\"photon-tag-tool\" ng-click=\"editTag(tag)\"><i class=\"icon-edit\"> </i></span>" +
                                "<span class=\"photon-tag-tool\" ng-click=\"deleteTag(tag)\"><i class=\"icon-trash\"> </i></span>" +
                            "</div>" +
                        "</div>" +
                        "<img class='photon-image' src='{{src}}' width='{{model.config.imageWidth}}' ng-mousedown=\"deselectCurrentTag($event);\"  />" +
                    "</div>" +
                "</div>",
            scope: {
                model: '=',
                src: '@'
            },
            link: link
        };

    }]);

/* ===================================== *\
    Services
\* ===================================== */

angular.module('umbraco.services').factory('Our.Umbraco.Services.photonMetaDataDialogService',
    function (dialogService, editorState) {
        return {
            open: function (options) {

                var currentEditorState = editorState.current;
                var callback = function () {
                    // We create a new editor state in the dialog,
                    // so be sure to set the previous one back 
                    // when we are done.
                    editorState.set(currentEditorState);
                };

                var o = $.extend({}, {
                    template: "/App_Plugins/Photon/Views/photon.metaDataDialog.html",
                    show: true,
                    requireName: true,
                }, options);


                // Wrap callbacks and reset the editor state
                if ("callback" in o) {
                    var oldCallback = o.callback;
                    o.callback = function (data) {
                        oldCallback(data);
                        callback(data);
                    };
                } else {
                    o.callback = callback;
                }

                if ("closeCallback" in o) {
                    var oldCloseCallback = o.closeCallback;
                    o.closeCallback = function (data) {
                        oldCloseCallback(data);
                        callback(data);
                    };
                } else {
                    o.closeCallback = callback;
                }

                // Launch the dialog
                dialogService.open(o);
            }
        };
    });

/* ===================================== *\
    Helpers
\* ===================================== */

function guid() {
    function _p8(s) {
        var p = (Math.random().toString(16) + "000000000").substr(2, 8);
        return s ? "-" + p.substr(0, 4) + "-" + p.substr(4, 4) : p;
    }

    return _p8() + _p8(true) + _p8(true) + _p8();
}