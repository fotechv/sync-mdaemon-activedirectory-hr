import * as React from "react";
import { connect, connectAdvanced } from "react-redux";
import { RouteComponentProps } from "react-router";
import { Link } from "react-router-dom";
import { ApplicationState } from "../store";
import * as EmployeesStore from "../store/EmployeeOut";
import * as Employee from "../models/Employee";
var dateFormat = require("dateformat");

// At runtime, Redux will merge together...
type EmployeeOutProps = EmployeesStore.IEmployeesOutState & // ... state we've requested from the Redux store
  typeof EmployeesStore.actionCreators & // ... plus action creators we've requested
  RouteComponentProps<{ startDateIndex: string }>; // ... plus incoming routing parameters

class EmployeeData extends React.PureComponent<EmployeeOutProps> {
  // This method is called when the component is first added to the document
  public componentDidMount() {
    this.ensureDataFetched();
  }

  // This method is called when the route parameters change
  public componentDidUpdate() {
    this.ensureDataFetched();
  }

  public render() {
    return (
      <React.Fragment>
        <h1 id="tabelLabel">Danh sách nhân sự nghỉ việc</h1>
        <p>Danh sách nhân sự trong 3 tháng gần nhất</p>
        {this.renderEmployeeLogTable()}
        {/* {this.renderPagination()} */}
      </React.Fragment>
    );
  }

  private ensureDataFetched() {
    const startDateIndex =
      parseInt(this.props.match.params.startDateIndex, 10) || 0;
    this.props.requestEmployees(startDateIndex);
  }

  private getLinkHrm(nhanVienId: number) {
    return (
      "https://hrm.haiphatland.com.vn/HRIS/Profile/Index/" + String(nhanVienId)
    );
  }

  private renderEmployeeLogTable() {
    console.log("Bắt đầu render list nhân sự");

    return (
      <table className="table table-striped" aria-labelledby="tabelLabel">
        <thead>
          <tr>
            <th>Họ tên</th>
            <th>Mã NV</th>
            <th>Username</th>
            <th>Phòng ban</th>
            <th>Mã phòng ban</th>
            {/* <th>Ngày tạo</th> */}
          </tr>
        </thead>
        <tbody>
          {this.props.employeesOut.map((employee: Employee.IEmployeeModel) => (
            <tr key={employee.NhanVienId}>
              <td>
                <a target="_blank" href={this.getLinkHrm(employee.NhanVienId)}>
                  {employee.Ho} {employee.Ten}
                </a>
              </td>
              <td>{employee.MaNhanVien}</td>
              <td>{employee.TenDangNhap}</td>
              <td>{employee.TenPhongBan}</td>
              <td>{employee.MaPhongBan}</td>
              {/* <td>{dateFormat(employee.Da, "dd/mm/yyyy")}</td> */}
            </tr>
          ))}
        </tbody>
      </table>
    );
  }

  private renderPagination() {
    const prevStartDateIndex = (this.props.startDateIndex || 0) - 5;
    const nextStartDateIndex = (this.props.startDateIndex || 0) + 5;

    return (
      <div className="d-flex justify-content-between">
        <Link
          className="btn btn-outline-secondary btn-sm"
          to={`/employee-log/${prevStartDateIndex}`}
        >
          Previous
        </Link>
        {this.props.isLoading && <span>Loading...</span>}
        <Link
          className="btn btn-outline-secondary btn-sm"
          to={`/employee-log/${nextStartDateIndex}`}
        >
          Next
        </Link>
      </div>
    );
  }
}

export default connect(
  (state: ApplicationState) => state.employeesOut, // Selects which state properties are merged into the component's props
  EmployeesStore.actionCreators // Selects which action creators are merged into the component's props
)(EmployeeData as any); // eslint-disable-line @typescript-eslint/no-explicit-any
