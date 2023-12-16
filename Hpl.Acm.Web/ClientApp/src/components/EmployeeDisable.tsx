import * as React from "react";
import { connect, connectAdvanced } from "react-redux";
import { RouteComponentProps } from "react-router";
import { Link } from "react-router-dom";
import { ApplicationState } from "../store";
import * as EmployeesStore from "../store/EmployeeDisable";
import * as EmployeeDis from "../models/Employee";
var dateFormat = require("dateformat");

// At runtime, Redux will merge together...
type EmployeeDisProps = EmployeesStore.IEmployeesDisableState & // ... state we've requested from the Redux store
  typeof EmployeesStore.actionCreators & // ... plus action creators we've requested
  RouteComponentProps<{ startDateIndex: string }>; // ... plus incoming routing parameters

class EmployeeData extends React.PureComponent<EmployeeDisProps> {
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
        <h1 id="tabelLabel">Danh sách nhân sự đã DISABLE và XÓA MAIL</h1>
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

  private renderEmployeeLogTable() {
    // console.log("Bắt đầu render list nhân sự");
    return (
      <table className="table table-striped" aria-labelledby="tabelLabel">
        <thead>
          <tr>
            <th>Họ tên</th>
            <th>Mã NV</th>
            <th>Username</th>
            <th>Mã phòng ban</th>
            <th>AD</th>
            <th>Email</th>
            <th>Sale</th>
            <th>Ngày disable</th>
          </tr>
        </thead>
        <tbody>
          {this.props.employeesDis.map(
            (employee: EmployeeDis.IEmployeeDisable) => (
              <tr key={employee.NhanVienId}>
                <td>
                  <a target="_blank" href={employee.LinkHrm}>
                    {employee.Ho} {employee.Ten}
                  </a>
                </td>
                <td>{employee.MaNhanVien}</td>
                <td>{employee.UserName}</td>
                <td>{employee.MaPhongBanCap1}</td>
                <td>{employee.DisableAd}</td>
                <td>{employee.DeleteEmail}</td>
                <td>{employee.LockSaleOnline}</td>
                <td>{dateFormat(employee.DateCreated, "dd/mm/yyyy")}</td>
              </tr>
            )
          )}
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
          to={`/employee-dis/${prevStartDateIndex}`}
        >
          Previous
        </Link>
        {this.props.isLoading && <span>Loading...</span>}
        <Link
          className="btn btn-outline-secondary btn-sm"
          to={`/employee-dis/${nextStartDateIndex}`}
        >
          Next
        </Link>
      </div>
    );
  }
}

export default connect(
  (state: ApplicationState) => state.employeesDisable, // Selects which state properties are merged into the component's props
  EmployeesStore.actionCreators // Selects which action creators are merged into the component's props
)(EmployeeData as any); // eslint-disable-line @typescript-eslint/no-explicit-any
