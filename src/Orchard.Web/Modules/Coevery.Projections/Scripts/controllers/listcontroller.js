﻿'use strict';

define(['core/app/detourService',
        'Modules/Coevery.Projections/Scripts/services/projectiondataservice'], function(detour) {
            detour.registerController([
                'ProjectionListCtrl',
                ['$rootScope', '$scope', 'logger', '$detour', '$resource', '$stateParams', 'projectionDataService',
                    function($rootScope, $scope, logger, $detour, $resource, $stateParams, projectionDataService) {

                        var t = function(str) {
                            var result = i18n.t(str);
                            return result;
                        };

                        var columnDefs = [
                            { field: 'ContentId', displayName: 'Actions', width: 100, cellTemplate: '<div class="ngCellText" ng-class="col.colIndex()"><a ng-click="edit(row.getProperty(col.field))">Edit</a>&nbsp;<a ng-click="delete(row.getProperty(col.field))">Delete</a></div>' },
                            { field: 'ContentId', displayName: t('Id') },
                            { field: 'EntityType', displayName: t('EntityType') },
                            { field: 'DisplayName', displayName: t('DisplayName') }];
                        $scope.mySelections = [];

                        $scope.gridOptions = {
                            data: 'myData',
                            //showSelectionCheckbox: true,
                            selectedItems: $scope.mySelections,
                            multiSelect: true,
                            enableColumnResize: true,
                            enableColumnReordering: true,
                            columnDefs: columnDefs
                        };

                        $scope.delete = function(id) {
                            projectionDataService.delete({ Id: id }, function() {
                                $scope.getAll();
                                logger.success('Delete the ' + id + ' successful.');
                            }, function() {
                                logger.error('Failed to delete the ' + id);
                            });
                        };

                        $scope.add = function() {
                            $detour.transitionTo('ProjectionCreate', { EntityName: $stateParams.EntityName });
                        };

                        $scope.edit = function(id) {
                            $detour.transitionTo('ProjectionEdit', { EntityName: $stateParams.EntityName, Id: id });
                        };

                        $scope.getAll = function() {
                            var records = projectionDataService.query({ Name: $stateParams.EntityName }, function () {
                                $scope.myData = records;
                            }, function() {
                                logger.error("Failed to fetched projections for " + $stateParams.EntityName);
                            });
                        };

                        $scope.getAll();
                    }]
            ]);
        });