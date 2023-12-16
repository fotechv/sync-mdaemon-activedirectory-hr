import * as React from "react";
import { connect, connectAdvanced } from "react-redux";
import { RouteComponentProps } from "react-router";
import { Link } from "react-router-dom";
import { ApplicationState } from "../store";
import * as DepartmentAcmStore from "../store/DepartmentAcm";
import * as Department from "../models/Department";

// At runtime, Redux will merge together...
type DepartmentAcmProps = DepartmentAcmStore.IDepartmentAcmState & // ... state we've requested from the Redux store
  typeof DepartmentAcmStore.actionCreators & // ... plus action creators we've requested
  RouteComponentProps<{ startDateIndex: string }>; // ... plus incoming routing parameters

class DepartmentAcmData extends React.PureComponent<DepartmentAcmProps> {
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
        <h1 id="tabelLabel">DANH SÁCH PHÒNG BAN CẤP 1</h1>
        <p>"Mã phòng ban" và "Branch code" phải trùng nhau</p>
        {this.renderDepartmentAcmTable()}
        {/* {this.renderPagination()} */}
      </React.Fragment>
    );
  }

  private ensureDataFetched() {
    const startDateIndex =
      parseInt(this.props.match.params.startDateIndex, 10) || 0;
    this.props.requestDepartmentAcms(startDateIndex);
  }

  private renderDepartmentAcmTable() {
    console.log("Bắt đầu render list");
    return (
      <table className="table table-striped" aria-labelledby="tabelLabel">
        <thead>
          <tr>
            <th>Tên phòng ban</th>
            <th>Mã phòng ban</th>
            <th>Branch name</th>
            <th>Branch code</th>
            <th>Mail group</th>
            <th>Email trợ lý</th>
          </tr>
        </thead>
        <tbody>
          {this.props.departmentAcms.map(
            (department: Department.IDepartmentAcm) => (
              <tr key={department.Id}>
                <td>{department.TenPhongBan}</td>
                <td>{department.MaPhongBan}</td>
                <td>{department.BranchName}</td>
                <td>{department.BranchCode}</td>
                <td>{department.MailingList}</td>
                <td>{department.EmailNotification}</td>
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
          to={`/department-config/${prevStartDateIndex}`}
        >
          Previous
        </Link>
        {this.props.isLoading && <span>Loading...</span>}
        <Link
          className="btn btn-outline-secondary btn-sm"
          to={`/department-config/${nextStartDateIndex}`}
        >
          Next
        </Link>
      </div>
    );
  }
}

export default connect(
  (state: ApplicationState) => {
    console.log("lỗi đây");
    state.departmentAcms, // Selects which state properties are merged into the component's props
      DepartmentAcmStore.actionCreators;
  } // Selects which action creators are merged into the component's props
)(DepartmentAcmData as any); // eslint-disable-line @typescript-eslint/no-explicit-any
