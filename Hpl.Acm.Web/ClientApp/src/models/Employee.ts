export interface IEmployeeLog {
  NhanVienId: number;
  FirstName: string;
  LastName: string;
  GioiTinh: string;
  MaNhanVien: string;
  TenDangNhap: string;
  Email: string;
  EmailCaNhan: string;
  DienThoai: string;
  Cmtnd: string;
  TenChucVu: string;
  TenChucDanh: string;
  PhongBanId: number;
  TenPhongBan: string;
  PhongBanCap1Id: number;
  TenPhongBanCap1: string;
  IsAd: string;
  IsHrm: string;
  IsSaleOnline: string;
  IsEmail: string;
  LinkHrm: string;
  LinkSaleOnline: string;
  LinkEmail: string;
  DateCreated: Date;
  // dateCreated: string;
}

export interface IEmployeeModel {
  NhanVienId: number;
  Ho: string;
  Ten: string;
  GioiTinh: string;
  MaNhanVien: string;
  TenDangNhap: string;
  Email: string;
  EmailCaNhan: string;
  DienThoai: string;
  CMTND: string;
  TenChucVu: string;
  TenChucDanh: string;
  PhongBanId: number;
  TenPhongBan: string;
  MaPhongBan: string;
  PhongBanCha: string;
  MaCha: string;
  PhongBanOng: string;
  MaOng: string;
  PhongBanCo: string;
  MaCo: string;
  PhongBanKy: string;
  MaKy: string;
  PhongBan6: string;
  MaPb6: string;
}

export interface IEmployeeDisable {
  Id: number;
  NhanVienId: number;
  MaNhanVien: string;
  Ho: string;
  Ten: string;
  UserName: string;
  Email: string;
  EmailCaNhan: string;
  DienThoai: string;
  Cmnd: string;
  PhongBanId: number;
  MaPhongBan: string;
  TenPhongBan: string;
  PhongBanCap1Id: number;
  MaPhongBanCap1: string;
  TenPhongBanCap1: string;
  DisableAd: string;
  DeleteEmail: string;
  LockSaleOnline: string;
  LinkHrm: string;
  DateCreated: Date;
  JsonLog: string;
}
