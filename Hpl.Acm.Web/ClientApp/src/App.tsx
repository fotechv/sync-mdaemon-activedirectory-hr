import * as React from "react";
import { Route } from "react-router";
import Layout from "./components/Layout";
import Home from "./components/Home";
import Counter from "./components/Counter";
import FetchData from "./components/FetchData";
import EmployeeLog from "./components/EmployeeLog";
import EmployeeDis from "./components/EmployeeDisable";
import EmployeeOut from "./components/EmployeeOut";
import DepartmentAcm from "./components/DepartmentAcm";

import "./custom.css";

export default () => (
  <Layout>
    <Route exact path="/" component={Home} />
    <Route path="/counter" component={Counter} />
    <Route path="/fetch-data/:startDateIndex?" component={FetchData} />
    <Route path="/department-acm/:startDateIndex?" component={DepartmentAcm} />
    <Route path="/employee-log/:startDateIndex?" component={EmployeeLog} />
    <Route path="/employee-dis/:startDateIndex?" component={EmployeeDis} />
    <Route path="/employee-out/:startDateIndex?" component={EmployeeOut} />
  </Layout>
);
