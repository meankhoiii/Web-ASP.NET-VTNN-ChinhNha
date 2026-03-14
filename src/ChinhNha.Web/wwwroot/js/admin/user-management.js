(function () {
    const token = document.querySelector('#antiForgeryForm input[name="__RequestVerificationToken"]')?.value || '';

    const endpoints = {
        index: '/Admin/User',
        create: '/Admin/User/Create',
        edit: '/Admin/User/Edit',
        detail: '/Admin/User/Detail',
        toggle: '/Admin/User/ToggleActive',
        deleteUser: '/Admin/User/Delete',
        bulkSetActive: '/Admin/User/BulkSetActive',
        changePassword: '/Admin/User/ChangePassword',
        exportExcel: '/Admin/User/ExportExcel'
    };

    $('.js-select2').select2({
        theme: 'bootstrap-5',
        width: '100%'
    });

    const table = $('#userTable').DataTable({
        processing: true,
        serverSide: true,
        autoWidth: false,
        responsive: true,
        pageLength: 10,
        ajax: {
            url: endpoints.index,
            data: function (d) {
                d.SearchTerm = $('#filterSearch').val();
                d.Role = $('#filterRole').val();
                d.ActiveStatus = $('#filterActive').val();
                d.CreatedFrom = $('#filterCreatedFrom').val();
                d.CreatedTo = $('#filterCreatedTo').val();
            }
        },
        columns: [
            {
                data: 'id',
                orderable: false,
                searchable: false,
                render: function (id) {
                    return '<input type="checkbox" class="row-check" value="' + id + '" />';
                }
            },
            {
                data: 'fullName',
                render: function (value, _, row) {
                    const avatar = row.avatarUrl || '/images/logo.png';
                    return '<div class="d-flex align-items-center gap-2">'
                        + '<img class="user-avatar" src="' + avatar + '" alt="avatar" />'
                        + '<span class="fw-semibold">' + escapeHtml(value || '') + '</span>'
                        + '</div>';
                }
            },
            { data: 'email' },
            {
                data: 'phone',
                render: function (value) {
                    return value || '<span class="text-muted">-</span>';
                }
            },
            {
                data: 'role',
                render: function (role) {
                    let cls = 'bg-secondary';
                    if (role === 'Admin') cls = 'bg-danger';
                    if (role === 'Staff') cls = 'bg-primary';
                    if (role === 'Customer') cls = 'bg-success';
                    return '<span class="badge user-badge-role ' + cls + '">' + escapeHtml(role || '') + '</span>';
                }
            },
            {
                data: 'isActive',
                render: function (active) {
                    return active
                        ? '<span class="badge bg-success">Active</span>'
                        : '<span class="badge bg-secondary">Inactive</span>';
                }
            },
            { data: 'createdAt' },
            {
                data: 'lastLoginAt',
                render: function (value) {
                    return value || '<span class="text-muted">-</span>';
                }
            },
            {
                data: null,
                orderable: false,
                searchable: false,
                render: function (_, __, row) {
                    const deleteDisabled = row.canDelete ? '' : 'disabled';
                    const deleteTitle = row.canDelete ? 'Xóa' : (row.cannotDeleteReason || 'Không thể xóa');
                    return ''
                        + '<button class="btn btn-outline-secondary btn-sm dt-action-btn btn-view" data-id="' + row.id + '" title="Chi tiết"><i class="fa-solid fa-eye"></i></button> '
                        + '<button class="btn btn-outline-primary btn-sm dt-action-btn btn-edit" data-id="' + row.id + '" title="Sửa"><i class="fa-solid fa-pen"></i></button> '
                        + '<button class="btn btn-outline-warning btn-sm dt-action-btn btn-toggle" data-id="' + row.id + '" title="Bật/tắt"><i class="fa-solid fa-power-off"></i></button> '
                        + '<button class="btn btn-outline-danger btn-sm dt-action-btn btn-delete" data-id="' + row.id + '" title="' + escapeHtml(deleteTitle) + '" ' + deleteDisabled + '><i class="fa-solid fa-trash"></i></button>';
                }
            }
        ],
        order: [[6, 'desc']],
        language: {
            processing: 'Đang tải...',
            search: 'Tìm nhanh:',
            lengthMenu: 'Hiển thị _MENU_ dòng',
            info: 'Hiển thị _START_ đến _END_ của _TOTAL_ dòng',
            infoEmpty: 'Không có dữ liệu',
            zeroRecords: 'Không tìm thấy người dùng',
            paginate: {
                first: 'Đầu',
                previous: 'Trước',
                next: 'Sau',
                last: 'Cuối'
            }
        }
    });

    $('#btnApplyFilter').on('click', function () {
        table.ajax.reload();
    });

    $('#filterSearch').on('keydown', function (e) {
        if (e.key === 'Enter') {
            e.preventDefault();
            table.ajax.reload();
        }
    });

    $('#chkAll').on('change', function () {
        $('.row-check').prop('checked', this.checked);
    });

    $('#userTable').on('draw.dt', function () {
        $('#chkAll').prop('checked', false);
    });

    $('#createUserForm').on('submit', function (e) {
        e.preventDefault();
        const formData = new FormData(this);
        formData.append('__RequestVerificationToken', token);

        $.ajax({
            url: endpoints.create,
            method: 'POST',
            data: formData,
            processData: false,
            contentType: false
        }).done(function (res) {
            if (!res.success) {
                return showError(res.message || 'Tạo người dùng thất bại.');
            }
            bootstrap.Modal.getOrCreateInstance(document.getElementById('createUserModal')).hide();
            document.getElementById('createUserForm').reset();
            showSuccess(res.message || 'Đã tạo người dùng.');
            table.ajax.reload(null, false);
        }).fail(function () {
            showError('Có lỗi xảy ra khi tạo người dùng.');
        });
    });

    $('#userTable').on('click', '.btn-edit', function () {
        const id = $(this).data('id');
        $.get(endpoints.edit, { id: id }).done(function (res) {
            if (!res.success) {
                return showError(res.message || 'Không tải được dữ liệu người dùng.');
            }

            const form = document.getElementById('editUserForm');
            form.reset();
            form.querySelector('[name="Id"]').value = res.data.id || '';
            form.querySelector('[name="FullName"]').value = res.data.fullName || '';
            form.querySelector('[name="Email"]').value = res.data.email || '';
            form.querySelector('[name="Phone"]').value = res.data.phone || '';
            form.querySelector('[name="Role"]').value = res.data.role || 'Customer';
            form.querySelector('[name="DateOfBirth"]').value = res.data.dateOfBirth || '';
            form.querySelector('[name="AvatarUrl"]').value = res.data.avatarUrl || '';
            form.querySelector('[name="IsActive"]').checked = !!res.data.isActive;

            bootstrap.Modal.getOrCreateInstance(document.getElementById('editUserModal')).show();
        }).fail(function () {
            showError('Không tải được dữ liệu người dùng.');
        });
    });

    $('#editUserForm').on('submit', function (e) {
        e.preventDefault();

        const form = this;
        const id = form.querySelector('[name="Id"]').value;
        const newPassword = form.querySelector('[name="NewPassword"]').value;
        const confirmNewPassword = form.querySelector('[name="ConfirmNewPassword"]').value;

        const updateData = new FormData(form);
        updateData.append('__RequestVerificationToken', token);

        $.ajax({
            url: endpoints.edit + '?id=' + encodeURIComponent(id),
            method: 'POST',
            data: updateData,
            processData: false,
            contentType: false
        }).done(function (res) {
            if (!res.success) {
                return showError(res.message || 'Cập nhật thất bại.');
            }

            if (!newPassword && !confirmNewPassword) {
                bootstrap.Modal.getOrCreateInstance(document.getElementById('editUserModal')).hide();
                showSuccess(res.message || 'Đã cập nhật người dùng.');
                table.ajax.reload(null, false);
                return;
            }

            $.post({
                url: endpoints.changePassword,
                data: {
                    __RequestVerificationToken: token,
                    id: id,
                    newPassword: newPassword,
                    confirmNewPassword: confirmNewPassword
                }
            }).done(function (pwdRes) {
                if (!pwdRes.success) {
                    return showError(pwdRes.message || 'Đổi mật khẩu thất bại.');
                }
                bootstrap.Modal.getOrCreateInstance(document.getElementById('editUserModal')).hide();
                showSuccess('Cập nhật user và đổi mật khẩu thành công.');
                table.ajax.reload(null, false);
            }).fail(function () {
                showError('Đổi mật khẩu thất bại.');
            });
        }).fail(function () {
            showError('Có lỗi xảy ra khi cập nhật người dùng.');
        });
    });

    $('#userTable').on('click', '.btn-view', function () {
        const id = $(this).data('id');
        $('#detailModalHost').load(endpoints.detail + '?id=' + encodeURIComponent(id), function (_, status) {
            if (status !== 'success') {
                return showError('Không tải được thông tin chi tiết.');
            }
            const modalEl = document.getElementById('detailUserModal');
            bootstrap.Modal.getOrCreateInstance(modalEl).show();
        });
    });

    $('#userTable').on('click', '.btn-toggle', function () {
        const id = $(this).data('id');

        $.post({
            url: endpoints.toggle,
            data: {
                __RequestVerificationToken: token,
                id: id
            }
        }).done(function (res) {
            if (!res.success) {
                return showError(res.message || 'Không thể cập nhật trạng thái.');
            }
            showSuccess(res.message || 'Đã cập nhật trạng thái tài khoản.');
            table.ajax.reload(null, false);
        }).fail(function () {
            showError('Không thể cập nhật trạng thái.');
        });
    });

    $('#userTable').on('click', '.btn-delete', function () {
        const id = $(this).data('id');
        Swal.fire({
            title: 'Xóa tài khoản?',
            text: 'Thao tác này không thể hoàn tác.',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonText: 'Xóa',
            cancelButtonText: 'Hủy',
            confirmButtonColor: '#dc3545'
        }).then(function (r) {
            if (!r.isConfirmed) return;

            $.post({
                url: endpoints.deleteUser,
                data: {
                    __RequestVerificationToken: token,
                    id: id
                }
            }).done(function (res) {
                if (!res.success) {
                    return showError(res.message || 'Xóa người dùng thất bại.');
                }
                showSuccess(res.message || 'Đã xóa người dùng.');
                table.ajax.reload(null, false);
            }).fail(function () {
                showError('Không thể xóa người dùng.');
            });
        });
    });

    $('#btnBulkActivate').on('click', function () {
        runBulkActive(true);
    });

    $('#btnBulkDeactivate').on('click', function () {
        runBulkActive(false);
    });

    $('#btnExportAll').on('click', function () {
        const query = buildFilterQuery();
        window.location.href = endpoints.exportExcel + (query ? ('?' + query) : '');
    });

    $('#btnExportSelected').on('click', function () {
        const ids = getSelectedIds();
        if (ids.length === 0) {
            return showError('Vui lòng chọn ít nhất 1 người dùng để xuất.');
        }

        const query = buildFilterQuery();
        const glue = query ? '&' : '';
        window.location.href = endpoints.exportExcel + '?' + query + glue + 'ids=' + encodeURIComponent(ids.join(','));
    });

    function runBulkActive(isActive) {
        const ids = getSelectedIds();
        if (ids.length === 0) {
            return showError('Vui lòng chọn ít nhất 1 người dùng.');
        }

        $.post({
            url: endpoints.bulkSetActive,
            data: {
                __RequestVerificationToken: token,
                ids: ids.join(','),
                isActive: isActive
            }
        }).done(function (res) {
            if (!res.success) {
                return showError(res.message || 'Không thể cập nhật hàng loạt.');
            }
            showSuccess(res.message || 'Đã cập nhật trạng thái hàng loạt.');
            table.ajax.reload(null, false);
        }).fail(function () {
            showError('Không thể cập nhật hàng loạt.');
        });
    }

    function getSelectedIds() {
        return $('.row-check:checked').map(function () { return $(this).val(); }).get();
    }

    function buildFilterQuery() {
        const params = new URLSearchParams();

        addIfNotEmpty(params, 'SearchTerm', $('#filterSearch').val());
        addIfNotEmpty(params, 'Role', $('#filterRole').val());
        addIfNotEmpty(params, 'ActiveStatus', $('#filterActive').val());
        addIfNotEmpty(params, 'CreatedFrom', $('#filterCreatedFrom').val());
        addIfNotEmpty(params, 'CreatedTo', $('#filterCreatedTo').val());

        return params.toString();
    }

    function addIfNotEmpty(params, key, value) {
        if (value !== null && value !== undefined && String(value).trim() !== '') {
            params.append(key, value);
        }
    }

    function showSuccess(message) {
        Swal.fire({ icon: 'success', title: 'Thành công', text: message, timer: 1800, showConfirmButton: false });
    }

    function showError(message) {
        Swal.fire({ icon: 'error', title: 'Có lỗi', text: message });
    }

    function escapeHtml(value) {
        return String(value)
            .replaceAll('&', '&amp;')
            .replaceAll('<', '&lt;')
            .replaceAll('>', '&gt;')
            .replaceAll('"', '&quot;')
            .replaceAll("'", '&#039;');
    }
})();
