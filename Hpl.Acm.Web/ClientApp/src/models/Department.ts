// "Id": 17,
// "PhongBanId": 308,
// "PhongBanParentId": 724,
// "MaPhongBan": "HPL08",
// "TenPhongBan": "BAN CHĂM SÓC KHÁCH HÀNG",
// "CreationTime": "2021-06-30T14:15:38.1166667",
// "MailingList": "bancskh@haiphatland.com.vn",
// "LastSyncToAd": "2021-06-30T14:15:38.1166667",
// "BranchId": 209,
// "BranchCode": "HPL08",
// "BranchName": "BAN CHĂM SÓC KHÁCH HÀNG",
// "EmailNotification": "haintm@haiphatland.com.vn"

export interface IDepartmentAcm {
  Id: number;
  PhongBanId: number;
  PhongBanParentId: number;
  MaPhongBan: string;
  TenPhongBan: string;
  CreationTime: Date;
  MailingList: string;
  LastSyncToAd: Date;
  BranchId: number;
  BranchCode: string;
  BranchName: string;
  EmailNotification: string;
}

export interface IDepartment {
  Id: number;
  PhongBanId: number;
  PhongBanParentId: number;
  MaPhongBan: string;
  TenPhongBan: string;
  CreationTime: Date;
  MailingList: string;
  LastSyncToAd: Date;
  BranchId: number;
  BranchCode: string;
  BranchName: string;
  EmailNotification: string;
}
