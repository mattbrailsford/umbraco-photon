/* ===================================== *\
    Controllers
\* ===================================== */

angular.module('umbraco').controller("Our.Umbraco.PropertyEditors.PhotonController",
    function($rootScope, $routeParams, $scope, $log, mediaHelper, cropperHelper, $timeout,
        editorState, umbRequestHelper, fileManager) {

        var config = angular.copy($scope.model.config);
        var defaultValue = { src: "", tags:[] };

        if ($scope.model.value)
        {
            $scope.imageSrc = $scope.model.value.src;
        }

        $scope.clear = function () {
            if (confirm("You sure?")) {
                //clear current uploaded files
                fileManager.setFiles($scope.model.alias, []);

                //clear the ui
                $scope.imageSrc = undefined;
                if ($scope.model.value) {
                    delete $scope.model.value;
                }
            }
        };

        $scope.done = function () {
            $scope.currentTag = undefined;
        };

        // on image selected, update the img tag
        $scope.$on("filesSelected", function (ev, args) {

            $scope.model.value = $.extend({}, defaultValue);

            if (args.files && args.files[0]) {
                fileManager.setFiles($scope.model.alias, args.files);

                var reader = new FileReader();
                reader.onload = function (e) {
                    $scope.$apply(function () {
                        $scope.imageSrc = e.target.result;
                    });
                };

                reader.readAsDataURL(args.files[0]);
            }
        });

        //here we declare a special method which will be called whenever the value has changed from the server
        $scope.model.onValueChanged = function (newVal, oldVal) {
            //clear current uploaded files
            fileManager.setFiles($scope.model.alias, []);
        };

        var unsubscribe = $scope.$on("formSubmitting", function () {
            $scope.done();
        });

        $scope.$on('$destroy', function () {
            unsubscribe();
        });
    });

angular.module("umbraco").controller("Our.Umbraco.Dialogs.PhotonMetaDataController",
    [
        "$scope",
        "editorState",
        "contentResource",

        function ($scope, editorState, contentResource) {

            $scope.node = null;

            $scope.save = function () {

                if (!$scope.photonForm.$valid)
                    return;

                // Copy property values to scope model value
                if ($scope.node) {
                    var value = { };
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
                contentResource.getScaffold(-20, $scope.dialogData.metaDataDocType).then(function (data)
                {
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
    
    var updateImageDimensions = function(url, imgWidth) {
        var tmpImg = new Image();
        tmpImg.src = url;
        $(tmpImg).one('load', function ()
        {
            origImageWidth = tmpImg.width;
            origImageHeight = tmpImg.height;

            imageWidth = imgWidth;
            imageHeight = Math.round((imageWidth / origImageWidth) * origImageHeight);

            delete tmpImg;
        });
    }

    var percToPx = function(val, maxVal) {
        return (maxVal / 100) * val;
    };

    var pxToPerc = function (val, maxVal) {
        return (100 / maxVal) * val;
    };

    var link = function ($scope, element, attrs, ctrl) {

        $scope.model.value.tags = $scope.model.value.tags || [];
        $scope.currentTag = null;

        var initImgAreaSelect = function() {
            ias = $(element).find(".photon-image").imgAreaSelect({
                hide: true,
                handles: false,
                instance: true,
                parent: $(element).find(".photon-image-wrapper"),
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

                    var tag = $scope.currentTag || { id: guid() };

                    tag.x = pxToPerc(sel.x1, imageWidth);
                    tag.y = pxToPerc(sel.y1, imageHeight);
                    tag.width = pxToPerc(sel.width, imageWidth);
                    tag.height = pxToPerc(sel.height, imageHeight);
                    tag.metaData = {};

                    if ($scope.currentTag == null) {
                        $scope.$apply(function () {
                            $scope.model.value.tags.push(tag);
                            $scope.currentTag = tag;
                        });
                    };
                }
            });
        }

        $scope.isCurrentTag = function (tag) {
            var isCurrentTag = $scope.currentTag != null && tag.id == $scope.currentTag.id;
            return isCurrentTag;
        }

        $scope.selectTag = function(tag, el) {
            $scope.currentTag = tag;
        }

        $scope.editCurrentTag = function () {
            metaDataDialogService.open({
                dialogData: {
                    metaDataDocType: $scope.model.config.metaDataDocType,
                    value: $scope.currentTag.metaData
                },
                callback: function (data) {
                    $scope.currentTag.metaData = data.value;
                }
            });
        }

        $scope.deleteCurrentTag = function () {
            $scope.model.value.tags = $.grep($scope.model.value.tags, function(itm, idx) {
                return !$scope.isCurrentTag(itm);
            });
            $scope.currentTag = null;
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

        $scope.$on('$destroy', function () {
            ias.setOptions({ remove: true });
        });
    }

    return {
        restrict: "E",
        replace: true, 
        template: "<div>" +
            "<div class='photon-image-wrapper' style=\"background-color:{{model.config.backgroundColor}};\">" +
            "<a class='ias_tag' ng-repeat=\"tag in model.value.tags\" ng-class=\"{active:tag.id==currentTag.id}\" ng-mousedown=\"selectTag(tag);\" style=\"width:{{tag.width}}%;height:{{tag.height}}%;left:{{tag.x}}%;top:{{tag.y}}%;\"></a>" +
            "<img class='photon-image' src='{{src}}' width='{{model.config.imageWidth}}' />" +
            "</div><br />"+
            "<a class=\"btn btn-link\" ng-disabled=\"!currentTag\" ng-click=\"editCurrentTag()\"><i class=\"icon-edit\"></i> Edit Tag Meta Data</a>"+
            "<a class=\"btn btn-link\" ng-disabled=\"!currentTag\" ng-click=\"deleteCurrentTag()\"><i class=\"icon-delete red\"></i> Delete Tag</a>" +
            "<hr style=\"margin: 10px 0;\" />"+
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

function guid()
{
    function _p8(s) {
        var p = (Math.random().toString(16) + "000000000").substr(2, 8);
        return s ? "-" + p.substr(0, 4) + "-" + p.substr(4, 4) : p;
    }

    return _p8() + _p8(true) + _p8(true) + _p8();
}